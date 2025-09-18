using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PinionCore.Extensions;
using PinionCore.Memorys;
using PinionCore.Network;
using PinionCore.Remote.Actors;
using PinionCore.Remote.Gateway.Backends;

namespace PinionCore.Remote.Gateway.Frontends
{
    class SessionInfo
    {
        public BackendClient Connector { get; set; }
        public uint Group { get; set; }
    }

    class User : IDisposable
    {
        private const int SessionBufferSize = 4096;

        private sealed class SessionState : IDisposable
        {
            public SessionState(SessionInfo info, CancellationToken parentToken)
            {
                Info = info ?? throw new ArgumentNullException(nameof(info));
                Stream = new Stream();
                ConnectorStream = new ReverseStream(Stream);
                Cancellation = CancellationTokenSource.CreateLinkedTokenSource(parentToken);
            }

            public SessionInfo Info { get; }
            public Stream Stream { get; }
            public IStreamable ConnectorStream { get; }
            public CancellationTokenSource Cancellation { get; }
            public Task PumpTask { get; set; }
            public bool Joined { get; set; }

            public void Dispose()
            {
                Cancellation.Cancel();
                try
                {
                    PumpTask?.Wait(TimeSpan.FromSeconds(1));
                }
                catch (AggregateException ex)
                {
                    ex.Handle(e => e is OperationCanceledException);
                }

                Cancellation.Dispose();
            }
        }

        private readonly IStreamable _client;
        private readonly HashSet<SessionInfo> _groups;
        private readonly Dictionary<uint, SessionState> _sessionStates;
        private readonly object _sync;
        private readonly IPool _pool;
        private readonly Serializer _serializer;
        private readonly PackageReader _clientReader;
        private readonly PackageSender _clientSender;
        private readonly DataflowActor<Package> _serverToClientActor;
        private readonly DataflowActor<Package> _clientToServerActor;
        private CancellationTokenSource _cancellation;
        private Task _clientPumpTask;
        private bool _started;
        private bool _disposed;

        public User(IStreamable streamable)
        {
            _client = streamable ?? throw new ArgumentNullException(nameof(streamable));
            _groups = new HashSet<SessionInfo>();
            _sessionStates = new Dictionary<uint, SessionState>();
            _sync = new object();

            _pool = PoolProvider.Shared;
            _serializer = new Serializer(_pool, new[]
            {
                typeof(Package),
                typeof(uint),
                typeof(byte[]),
                typeof(byte)
            });

            _clientReader = new PackageReader(_client, _pool);
            _clientSender = new PackageSender(_client, _pool);

            _serverToClientActor = new DataflowActor<Package>(ForwardPackageToClientAsync, new ActorOptions
            {
                DisposeTimeout = TimeSpan.FromSeconds(1)
            });

            _clientToServerActor = new DataflowActor<Package>(ForwardPackageToServiceAsync, new ActorOptions
            {
                DisposeTimeout = TimeSpan.FromSeconds(1)
            });
        }

        public void AddGroup(SessionInfo group)
        {
            if (_groups.Add(group) && _started)
            {
                _Join(group);
            }
        }

        private void _Join(SessionInfo group)
        {
            SessionState state;
            lock (_sync)
            {
                if (!_started || _sessionStates.ContainsKey(group.Group))
                {
                    return;
                }

                state = new SessionState(group, _cancellation.Token);
                _sessionStates.Add(group.Group, state);
            }

            try
            {
                if (_cancellation.IsCancellationRequested)
                {
                    RemoveState(group.Group, state);
                    return;
                }

                _ = group.Connector.Join(state.ConnectorStream);
                state.Joined = true;
                state.PumpTask = Task.Run(() => PumpConnectorAsync(state, state.Cancellation.Token));
            }
            catch
            {
                RemoveState(group.Group, state);
                throw;
            }
        }

        private void RemoveState(uint group, SessionState state)
        {
            lock (_sync)
            {
                _sessionStates.Remove(group);
            }

            state.Dispose();
        }

        public bool HasGroup(uint group)
        {
            return _groups.Any(g => g.Group == group);
        }

        internal void RemoveGroup(uint group)
        {
            SessionState state = null;

            lock (_sync)
            {
                var session = _groups.FirstOrDefault(g => g.Group == group);
                if (session != null)
                {
                    _groups.Remove(session);
                }

                if (_sessionStates.TryGetValue(group, out state))
                {
                    _sessionStates.Remove(group);
                }
            }

            if (state == null)
            {
                return;
            }

            if (_started && state.Joined)
            {
                try
                {
                    state.Info.Connector.Leave(state.ConnectorStream);
                }
                catch (Exception)
                {
                }
            }

            state.Dispose();
        }

        internal void Stop()
        {
            if (!_started)
            {
                return;
            }

            _started = false;
            _cancellation?.Cancel();

            List<SessionState> states;
            lock (_sync)
            {
                states = _sessionStates.Values.ToList();
                _sessionStates.Clear();
                _groups.Clear();
            }

            foreach (var state in states)
            {
                if (state.Joined)
                {
                    try
                    {
                        state.Info.Connector.Leave(state.ConnectorStream);
                    }
                    catch (Exception)
                    {
                    }
                }

                state.Dispose();
            }

            if (_clientPumpTask != null)
            {
                try
                {
                    _clientPumpTask.Wait(TimeSpan.FromSeconds(1));
                }
                catch (AggregateException ex)
                {
                    ex.Handle(e => e is OperationCanceledException);
                }

                _clientPumpTask = null;
            }

            _cancellation?.Dispose();
            _cancellation = null;
        }

        public bool Equals(IStreamable other)
        {
            return _client == other;
        }

        internal void Start()
        {
            if (_started)
            {
                return;
            }

            _started = true;
            _cancellation = new CancellationTokenSource();
            _clientPumpTask = Task.Run(() => PumpClientAsync(_cancellation.Token));

            List<SessionInfo> toJoin;
            lock (_sync)
            {
                toJoin = _groups.ToList();
            }

            foreach (var session in toJoin)
            {
                _Join(session);
            }
        }

        private async Task PumpConnectorAsync(SessionState state, CancellationToken token)
        {
            var buffer = new byte[SessionBufferSize];
            var stream = (IStreamable)state.Stream;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    var count = await stream.Receive(buffer, 0, buffer.Length);
                    if (count <= 0)
                    {
                        continue;
                    }

                    var payload = new byte[count];
                    Array.Copy(buffer, 0, payload, 0, count);

                    var package = new Package
                    {
                        ServiceId = state.Info.Group,
                        Payload = payload
                    };

                    var accepted = await _serverToClientActor.SendAsync(package, token).ConfigureAwait(false);
                    if (!accepted)
                    {
                        throw new InvalidOperationException("Server forwarding actor declined processing.");
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _serverToClientActor.Fault(ex);
                throw;
            }
        }

        private async Task PumpClientAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var buffers = await _clientReader.Read().ConfigureAwait(false);
                    if (buffers == null || buffers.Count == 0)
                    {
                        continue;
                    }

                    foreach (var buffer in buffers)
                    {
                        var deserialized = _serializer.Deserialize(buffer);
                        if (deserialized is Package package)
                        {
                            var accepted = await _clientToServerActor.SendAsync(package, token).ConfigureAwait(false);
                            if (!accepted)
                            {
                                throw new InvalidOperationException("Client forwarding actor declined processing.");
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _clientToServerActor.Fault(ex);
                throw;
            }
        }

        private Task ForwardPackageToClientAsync(Package package, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var normalized = new Package
            {
                ServiceId = package.ServiceId,
                Payload = package.Payload ?? Array.Empty<byte>()
            };

            var buffer = _serializer.Serialize(normalized);
            _clientSender.Push(buffer);
            return Task.CompletedTask;
        }

        private async Task ForwardPackageToServiceAsync(Package package, CancellationToken token)
        {
            SessionState state;
            lock (_sync)
            {
                _sessionStates.TryGetValue(package.ServiceId, out state);
            }

            if (state == null || !state.Joined)
            {
                return;
            }

            var payload = package.Payload ?? Array.Empty<byte>();
            if (payload.Length == 0)
            {
                return;
            }

            var stream = (IStreamable)state.Stream;
            var offset = 0;

            while (offset < payload.Length)
            {
                token.ThrowIfCancellationRequested();

                var written = await stream.Send(payload, offset, payload.Length - offset);
                if (written <= 0)
                {
                    break;
                }

                offset += written;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            Stop();
            if (_clientSender is IDisposable senderDisposable)
            {
                senderDisposable.Dispose();
            }
            _serverToClientActor.Dispose();
            _clientToServerActor.Dispose();
        }
    }

    class FrontServer : IDisposable
    {
        readonly List<SessionInfo> _Sessions;
        readonly List<User> _Users;

        public FrontServer()
        {
            _Sessions = new List<SessionInfo>();
            _Users = new List<User>();
        }

        internal void Register(uint group, BackendClient connector)
        {
            var info = new SessionInfo { Group = group, Connector = connector };
            _Sessions.Add(info);

            _NotifyUsersAdd(info);
        }
        internal void Unregister(BackendClient connector)
        {
            var session = _Sessions.FirstOrDefault(s => s.Connector == connector);
            if (session != null)
            {
                _Sessions.Remove(session);
                _NotifyUsersRemove(session);
            }
        }

        private void _NotifyUsersRemove(SessionInfo session)
        {
            foreach (var user in _Users)
            {
                if (user.HasGroup(session.Group))
                {
                    user.RemoveGroup(session.Group);
                }
            }
        }

        private void _NotifyUsersAdd(SessionInfo info)
        {
            foreach (var user in _Users)
            {
                if (!user.HasGroup(info.Group))
                {
                    user.AddGroup(info);
                }
            }
        }
        public void Join(IStreamable streamable)
        {
            var user = new User(streamable);

            foreach (var session in _Sessions.Shuffle())
            {
                if (!user.HasGroup(session.Group))
                {
                    user.AddGroup(session);
                }
            }
            user.Start();
            _Users.Add(user);
        }

        public void Leave(IStreamable streamable)
        {
            var user = _Users.FirstOrDefault(u => u.Equals(streamable));
            if (user != null)
            {
                user.Dispose();
                _Users.Remove(user);
            }
        }

        void IDisposable.Dispose()
        {
            foreach (var user in _Users)
            {
                user.Dispose();
            }

            _Users.Clear();
        }
    }
}
