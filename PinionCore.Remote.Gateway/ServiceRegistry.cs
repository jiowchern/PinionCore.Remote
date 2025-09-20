using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PinionCore.Memorys;
using PinionCore.Network;
using PinionCore.Remote.Actors;

namespace PinionCore.Remote.Gateway
{
    internal sealed class ServiceRegistry : IDisposable
    {
        private readonly IPool _pool;
        private readonly Serializer _serializer;
        private readonly DataflowActor<Func<Task>> _actor;
        private readonly Dictionary<uint, List<ServiceSession>> _servicesByGroup;
        private readonly Dictionary<IStreamable, ServiceSession> _servicesByStream;
        private readonly Dictionary<uint, UserSession> _usersById;
        private readonly Dictionary<uint, Dictionary<uint, ServiceSession>> _serviceByUserAndGroup;
        private bool _disposed;

        public ServiceRegistry(IPool pool, Serializer serializer)
        {
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _servicesByGroup = new Dictionary<uint, List<ServiceSession>>();
            _servicesByStream = new Dictionary<IStreamable, ServiceSession>();
            _usersById = new Dictionary<uint, UserSession>();
            _serviceByUserAndGroup = new Dictionary<uint, Dictionary<uint, ServiceSession>>();
            _actor = new DataflowActor<Func<Task>>((func, _) => func());
        }

        public void Register(uint serviceGroup, IStreamable serviceStream)
        {
            if (serviceStream == null)
            {
                throw new ArgumentNullException(nameof(serviceStream));
            }

            _Execute(() => _RegisterCore(serviceGroup, serviceStream));
        }

        public void Unregister(uint serviceGroup, IStreamable serviceStream)
        {
            if (serviceStream == null)
            {
                throw new ArgumentNullException(nameof(serviceStream));
            }

            _Execute(() => _UnregisterCore(serviceGroup, serviceStream));
        }

        public void Join(UserSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            _Execute(() => _JoinCore(session));
        }

        public void Leave(UserSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            _Execute(() => _LeaveCore(session, true));
        }

        private void _RegisterCore(uint serviceGroup, IStreamable serviceStream)
        {
            _EnsureNotDisposed();
            if (_servicesByStream.ContainsKey(serviceStream))
            {
                throw new InvalidOperationException("Service stream already registered.");
            }

            var service = new ServiceSession(serviceGroup, serviceStream, _pool, _serializer);
            service.MessageReceived += _OnServiceSessionMessage;
            service.Disconnected += _OnServiceSessionDisconnected;
            service.Start();

            if (!_servicesByGroup.TryGetValue(serviceGroup, out var list))
            {
                list = new List<ServiceSession>();
                _servicesByGroup.Add(serviceGroup, list);
            }

            list.Add(service);
            _servicesByStream.Add(serviceStream, service);

            foreach (var session in _usersById.Values)
            {
                if (!_IsUserAssignedToGroup(session.Id, serviceGroup))
                {
                    _AssignUserToGroup(session, serviceGroup, service);
                }
            }
        }

        private void _UnregisterCore(uint serviceGroup, IStreamable serviceStream)
        {
            _EnsureNotDisposed();
            if (!_servicesByStream.TryGetValue(serviceStream, out var service) || service.Group != serviceGroup)
            {
                throw new InvalidOperationException("Service stream not registered for the specified group.");
            }

            _RemoveService(service, true);
        }

        private void _JoinCore(UserSession session)
        {
            _EnsureNotDisposed();
            if (_usersById.ContainsKey(session.Id))
            {
                throw new InvalidOperationException("Session already joined.");
            }

            _usersById.Add(session.Id, session);
            _serviceByUserAndGroup.Add(session.Id, new Dictionary<uint, ServiceSession>());

            session.MessageReceived += _OnUserSessionMessage;
            session.Disconnected += _OnUserSessionDisconnected;
            session.Start();

            _AssignUserToExistingGroups(session);
        }

        private void _LeaveCore(UserSession session, bool notifyService)
        {
            if (!_usersById.Remove(session.Id))
            {
                return;
            }

            session.MessageReceived -= _OnUserSessionMessage;
            session.Disconnected -= _OnUserSessionDisconnected;

            if (_serviceByUserAndGroup.TryGetValue(session.Id, out var groupMap))
            {
                foreach (var kvp in groupMap.ToArray())
                {
                    var group = kvp.Key;
                    var service = kvp.Value;
                    service.RemoveUser(session.Id);

                    if (notifyService)
                    {
                        var package = new ServiceRegistryPackage
                        {
                            OpCode = OpCodeFromServiceRegistry.Leave,
                            UserId = session.Id,
                            Payload = Array.Empty<byte>()
                        };

                        _TrySend(service, package);
                    }
                }

                _serviceByUserAndGroup.Remove(session.Id);
            }

            session.Dispose();
        }

        private void _AssignUserToExistingGroups(UserSession session)
        {
            foreach (var group in _servicesByGroup.Keys.ToArray())
            {
                _AssignUserToGroup(session, group);
            }
        }

        private bool _AssignUserToGroup(UserSession session, uint group, ServiceSession preferred = null)
        {
            if (!_servicesByGroup.TryGetValue(group, out var services) || services.Count == 0)
            {
                return false;
            }

            if (!_serviceByUserAndGroup.TryGetValue(session.Id, out var groupMap))
            {
                groupMap = new Dictionary<uint, ServiceSession>();
                _serviceByUserAndGroup[session.Id] = groupMap;
            }

            if (groupMap.ContainsKey(group))
            {
                return true;
            }

            var service = preferred ?? _SelectService(services);
            if (service == null)
            {
                return false;
            }

            groupMap[group] = service;
            service.AddUser(session.Id);

            var package = new ServiceRegistryPackage
            {
                OpCode = OpCodeFromServiceRegistry.Join,
                UserId = session.Id,
                Payload = Array.Empty<byte>()
            };

            _TrySend(service, package);
            return true;
        }

        private static ServiceSession _SelectService(IReadOnlyList<ServiceSession> services)
        {
            ServiceSession selected = null;
            for (var i = 0; i < services.Count; i++)
            {
                var service = services[i];
                if (selected == null || service.UserCount < selected.UserCount)
                {
                    selected = service;
                }
            }

            return selected;
        }

        private void _OnUserSessionMessage(UserSession session, List<Memorys.Buffer> buffers)
        {
            if (buffers == null || buffers.Count == 0)
            {
                return;
            }

            var messages = _ExtractMessages(buffers);
            if (messages.Count == 0)
            {
                return;
            }

            _Queue(() => _ForwardUserPayloads(session, messages));
        }

        private void _ForwardUserPayloads(UserSession session, List<(uint Group, byte[] Payload)> messages)
        {
            if (!_serviceByUserAndGroup.TryGetValue(session.Id, out var groupMap))
            {
                return;
            }

            foreach (var message in messages)
            {
                if (!groupMap.TryGetValue(message.Group, out var service))
                {
                    continue;
                }

                var package = new ServiceRegistryPackage
                {
                    OpCode = OpCodeFromServiceRegistry.Message,
                    UserId = session.Id,
                    Payload = message.Payload
                };

                _TrySend(service, package);
            }
        }

        private static List<(uint Group, byte[] Payload)> _ExtractMessages(List<Memorys.Buffer> buffers)
        {
            var messages = new List<(uint Group, byte[] Payload)>(buffers.Count);
            foreach (var buffer in buffers)
            {
                var segment = buffer.Bytes;
                if (segment.Count < sizeof(uint))
                {
                    continue;
                }

                var group = BinaryPrimitives.ReadUInt32LittleEndian(segment.Array.AsSpan(segment.Offset, sizeof(uint)));
                var payloadLength = segment.Count - sizeof(uint);
                var payload = new byte[payloadLength];
                if (payloadLength > 0)
                {
                    Array.Copy(segment.Array, segment.Offset + sizeof(uint), payload, 0, payloadLength);
                }

                messages.Add((group, payload));
            }

            return messages;
        }

        private void _OnUserSessionDisconnected(UserSession session)
        {
            _Queue(() => _LeaveCore(session, true));
        }

        private void _OnServiceSessionMessage(ServiceSession service, SessionListenerPackage package)
        {
            _Queue(() => _DeliverServiceMessage(service, package));
        }

        private void _DeliverServiceMessage(ServiceSession service, SessionListenerPackage package)
        {
            if (package.OpCode != OpCodeFromSessionListener.Message)
            {
                return;
            }

            if (!_usersById.TryGetValue(package.UserId, out var session))
            {
                return;
            }

            if (!_serviceByUserAndGroup.TryGetValue(package.UserId, out var groupMap))
            {
                return;
            }

            if (!groupMap.TryGetValue(service.Group, out var owner) || !ReferenceEquals(owner, service))
            {
                return;
            }

            session.SendToUser(service.Group, package.Payload ?? Array.Empty<byte>());
        }

        private void _OnServiceSessionDisconnected(ServiceSession service)
        {
            _Queue(() => _RemoveService(service, false));
        }

        private void _RemoveService(ServiceSession service, bool notifyService)
        {
            if (service == null)
            {
                return;
            }

            if (!_servicesByStream.TryGetValue(service.Stream, out var tracked) || !ReferenceEquals(tracked, service))
            {
                return;
            }

            service.MessageReceived -= _OnServiceSessionMessage;
            service.Disconnected -= _OnServiceSessionDisconnected;

            if (_servicesByGroup.TryGetValue(service.Group, out var list))
            {
                list.Remove(service);
                if (list.Count == 0)
                {
                    _servicesByGroup.Remove(service.Group);
                }
            }

            _servicesByStream.Remove(service.Stream);

            var affectedUsers = service.DetachAllUsers();
            foreach (var userId in affectedUsers)
            {
                if (_serviceByUserAndGroup.TryGetValue(userId, out var map))
                {
                    map.Remove(service.Group);
                    if (map.Count == 0)
                    {
                        _serviceByUserAndGroup.Remove(userId);
                    }
                }

                if (_usersById.TryGetValue(userId, out var session))
                {
                    if (!_AssignUserToGroup(session, service.Group))
                    {
                        // TODO: queue the session for future reassignment when a service becomes available.
                    }
                }
            }

            if (notifyService)
            {
                foreach (var userId in affectedUsers)
                {
                    var package = new ServiceRegistryPackage
                    {
                        OpCode = OpCodeFromServiceRegistry.Leave,
                        UserId = userId,
                        Payload = Array.Empty<byte>()
                    };

                    _TrySend(service, package);
                }
            }

            service.Dispose();
        }

        private bool _IsUserAssignedToGroup(uint userId, uint group)
        {
            return _serviceByUserAndGroup.TryGetValue(userId, out var map) && map.ContainsKey(group);
        }

        private void _TrySend(ServiceSession service, ServiceRegistryPackage package)
        {
            try
            {
                service.Send(package);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void _Execute(Action action)
        {
            _EnsureNotDisposed();
            var tcs = new TaskCompletionSource<bool>();
            Func<Task> message = () =>
            {
                try
                {
                    action();
                    tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }

                return Task.CompletedTask;
            };

            _actor.SendAsync(message).GetAwaiter().GetResult();
            tcs.Task.GetAwaiter().GetResult();
        }

        private void _Queue(Action action)
        {
            if (_disposed)
            {
                return;
            }

            Func<Task> message = () =>
            {
                action();
                return Task.CompletedTask;
            };

            if (!_actor.Post(message))
            {
                _ = _actor.SendAsync(message);
            }
        }

        private void _EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ServiceRegistry));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            foreach (var session in _usersById.Values.ToArray())
            {
                session.MessageReceived -= _OnUserSessionMessage;
                session.Disconnected -= _OnUserSessionDisconnected;
                session.Dispose();
            }

            _usersById.Clear();
            _serviceByUserAndGroup.Clear();

            foreach (var service in _servicesByStream.Values.ToArray())
            {
                service.MessageReceived -= _OnServiceSessionMessage;
                service.Disconnected -= _OnServiceSessionDisconnected;
                service.Dispose();
            }

            _servicesByStream.Clear();
            _servicesByGroup.Clear();

            _actor.Dispose();
        }

        private sealed class ServiceSession : IDisposable
        {
            private readonly Channel _channel;
            private readonly Serializer _serializer;
            private readonly HashSet<uint> _userIds;
            private bool _disposed;

            public ServiceSession(uint group, IStreamable stream, IPool pool, Serializer serializer)
            {
                Group = group;
                Stream = stream ?? throw new ArgumentNullException(nameof(stream));
                _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
                _userIds = new HashSet<uint>();
                _channel = new Channel(new PackageReader(stream, pool ?? throw new ArgumentNullException(nameof(pool))), new PackageSender(stream, pool));
                _channel.OnDataReceived += _HandleIncoming;
                _channel.OnDisconnected += _HandleDisconnected;
            }

            public uint Group { get; }

            public IStreamable Stream { get; }

            public int UserCount => _userIds.Count;

            public event Action<ServiceSession, SessionListenerPackage> MessageReceived;

            public event Action<ServiceSession> Disconnected;

            public void Start()
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(ServiceSession));
                }

                _channel.Start();
            }

            public void AddUser(uint userId)
            {
                _userIds.Add(userId);
            }

            public bool RemoveUser(uint userId)
            {
                return _userIds.Remove(userId);
            }

            public IReadOnlyCollection<uint> DetachAllUsers()
            {
                var users = _userIds.ToArray();
                _userIds.Clear();
                return users;
            }

            public void Send(ServiceRegistryPackage package)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(ServiceSession));
                }

                var buffer = _serializer.Serialize(package);
                _channel.Sender.Push(buffer);
            }

            private List<Memorys.Buffer> _HandleIncoming(List<Memorys.Buffer> buffers)
            {
                foreach (var buffer in buffers)
                {
                    var package = (SessionListenerPackage)_serializer.Deserialize(buffer);
                    MessageReceived?.Invoke(this, package);
                }

                return new List<Memorys.Buffer>();
            }

            private void _HandleDisconnected()
            {
                Disconnected?.Invoke(this);
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _channel.OnDataReceived -= _HandleIncoming;
                _channel.OnDisconnected -= _HandleDisconnected;
                _channel.Dispose();
            }
        }
    }
}

