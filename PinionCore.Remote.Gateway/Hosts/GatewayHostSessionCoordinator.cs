using System;
using System.Collections.Generic;
using System.Linq;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Hosts
{
    interface ISessionMembership
    {
        void Join(IRoutableSession session);
        void Leave(IRoutableSession session);
    }

    interface IServiceRegistry
    {
        void Register(uint group, IGameLobby service);
        void Unregister(IGameLobby service);
    }
    internal class GatewayHostSessionCoordinator : ISessionMembership , IServiceRegistry
    {
        private sealed class SessionBinding
        {
            private readonly System.Collections.Generic.List<System.Action<uint>> _clientIdCallbacks;
            internal IRoutableSession Session { get; }
            internal ServiceRegistration Registration { get; }
            internal uint Group { get; }
            internal uint UserId { get; private set; }
            internal IClientConnection ClientConnection { get; set; }
            internal bool Bound { get; set; }
            internal bool Releasing { get; set; }
            internal bool HasUserId { get; private set; }

            internal SessionBinding(IRoutableSession session, ServiceRegistration registration, uint group)
            {
                Session = session;
                Registration = registration;
                Group = group;
                _clientIdCallbacks = new System.Collections.Generic.List<System.Action<uint>>();
            }

            internal void OnUserIdAssigned(System.Action<uint> callback)
            {
                if (HasUserId)
                {
                    callback(UserId);
                }
                else
                {
                    _clientIdCallbacks.Add(callback);
                }
            }

            internal void SetUserId(uint clientId)
            {
                if (HasUserId)
                {
                    return;
                }

                HasUserId = true;
                UserId = clientId;

                foreach (var callback in _clientIdCallbacks)
                {
                    callback(clientId);
                }

                _clientIdCallbacks.Clear();
            }
        }

        private sealed class ServiceRegistration
        {
            internal IGameLobby Service { get; }
            internal uint Group { get; }
            internal Dictionary<IRoutableSession, SessionBinding> SessionBindings { get; }
            internal Dictionary<uint, SessionBinding> BindingsByClientId { get; }
            internal Action<IClientConnection> SupplyHandler { get; }
            internal Action<IClientConnection> UnsupplyHandler { get; }
            internal Dictionary<uint, IClientConnection> PendingConnections { get; }

            internal ServiceRegistration(IGameLobby service, uint group, Action<IClientConnection> supplyHandler, Action<IClientConnection> unsupplyHandler)
            {
                Service = service;
                Group = group;
                SupplyHandler = supplyHandler;
                UnsupplyHandler = unsupplyHandler;
                SessionBindings = new Dictionary<IRoutableSession, SessionBinding>();
                BindingsByClientId = new Dictionary<uint, SessionBinding>();
                PendingConnections = new Dictionary<uint, IClientConnection>();
            }

            internal void Subscribe()
            {
                Service.ClientNotifier.Base.Supply += SupplyHandler;
                Service.ClientNotifier.Base.Unsupply += UnsupplyHandler;
            }

            internal void Unsubscribe()
            {
                Service.ClientNotifier.Base.Supply -= SupplyHandler;
                Service.ClientNotifier.Base.Unsupply -= UnsupplyHandler;
            }
        }

        private readonly object _syncRoot;
        private readonly HashSet<IRoutableSession> _sessions;
        private readonly Dictionary<IRoutableSession, Dictionary<uint, SessionBinding>> _sessionBindings;
        private readonly Dictionary<uint, List<ServiceRegistration>> _registrationsByGroup;
        private readonly Dictionary<IGameLobby, ServiceRegistration> _registrationsByService;
        private readonly IGameLobbySelectionStrategy _selectionStrategy;

        public GatewayHostSessionCoordinator(IGameLobbySelectionStrategy selectionStrategy = null)
        {
            _syncRoot = new object();
            _sessions = new HashSet<IRoutableSession>();
            _sessionBindings = new Dictionary<IRoutableSession, Dictionary<uint, SessionBinding>>();
            _registrationsByGroup = new Dictionary<uint, List<ServiceRegistration>>();
            _registrationsByService = new Dictionary<IGameLobby, ServiceRegistration>();
            _selectionStrategy = selectionStrategy ?? new RoundRobinGameLobbySelectionStrategy();
        }

        public void Join(IRoutableSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            lock (_syncRoot)
            {
                if (!_sessions.Add(session))
                {
                    return;
                }

                if (!_sessionBindings.ContainsKey(session))
                {
                    _sessionBindings[session] = new Dictionary<uint, SessionBinding>();
                }

                foreach (var group in _registrationsByGroup.Keys.ToArray())
                {
                    _EnsureSessionBinding(session, group);
                }
            }
        }

        public void Leave(IRoutableSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            lock (_syncRoot)
            {
                if (!_sessions.Remove(session))
                {
                    return;
                }

                if (!_sessionBindings.TryGetValue(session, out var groupBindings))
                {
                    return;
                }

                foreach (var binding in groupBindings.Values.ToList())
                {
                    _ReleaseBinding(binding, reassign: false);
                }

                _sessionBindings.Remove(session);
            }
        }

        public void Register(uint group, IGameLobby service)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            lock (_syncRoot)
            {
                if (_registrationsByService.ContainsKey(service))
                {
                    return;
                }

                var registration = new ServiceRegistration(
                    service,
                    group,
                    svc => _OnClientConnectionSupplied(service, svc),
                    svc => _OnClientConnectionUnsupplied(service, svc));

                _registrationsByService.Add(service, registration);

                if (!_registrationsByGroup.TryGetValue(group, out var registrations))
                {
                    registrations = new List<ServiceRegistration>();
                    _registrationsByGroup[group] = registrations;
                }

                registrations.Add(registration);
                registration.Subscribe();

                foreach (var session in _sessions)
                {
                    _EnsureSessionBinding(session, group);
                }
            }
        }

        public void Unregister(IGameLobby service)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            lock (_syncRoot)
            {
                if (!_registrationsByService.TryGetValue(service, out var registration))
                {
                    return;
                }

                registration.Unsubscribe();
                _registrationsByService.Remove(service);

                if (_registrationsByGroup.TryGetValue(registration.Group, out var registrations))
                {
                    registrations.Remove(registration);
                    if (registrations.Count == 0)
                    {
                        _registrationsByGroup.Remove(registration.Group);
                    }
                }

                foreach (var binding in registration.SessionBindings.Values.ToList())
                {
                    _ReleaseBinding(binding, reassign: true);
                }
            }
        }

        private void _EnsureSessionBinding(IRoutableSession session, uint group)
        {
            if (!_registrationsByGroup.TryGetValue(group, out var registrations) || registrations.Count == 0)
            {
                return;
            }

            if (!_sessionBindings.TryGetValue(session, out var groupBindings))
            {
                groupBindings = new Dictionary<uint, SessionBinding>();
                _sessionBindings[session] = groupBindings;
            }

            if (groupBindings.TryGetValue(group, out var current) && current != null && current.Bound)
            {
                return;
            }

            foreach (var registration in _GetOrderedRegistrations(registrations, group))
            {
                if (_TryAttach(registration, session, group, out var binding))
                {
                    groupBindings[group] = binding;
                    return;
                }
            }

            groupBindings.Remove(group);
        }

        private IEnumerable<ServiceRegistration> _GetOrderedRegistrations(List<ServiceRegistration> registrations, uint group)
        {
            if (registrations == null || registrations.Count == 0)
            {
                yield break;
            }

            var seen = new HashSet<ServiceRegistration>();

            var services = registrations.Select(r => r.Service).ToList();
            foreach (var service in _selectionStrategy.Select(group, services))
            {
                if (service == null)
                {
                    continue;
                }

                if (_registrationsByService.TryGetValue(service, out var registration) && registration.Group == group && seen.Add(registration))
                {
                    yield return registration;
                }
            }

            foreach (var registration in registrations)
            {
                if (seen.Add(registration))
                {
                    yield return registration;
                }
            }
        }

        private bool _TryAttach(ServiceRegistration registration, IRoutableSession session, uint group, out SessionBinding binding)
        {
            if (registration.SessionBindings.TryGetValue(session, out binding))
            {
                return true;
            }

            var newBinding = new SessionBinding(session, registration, group);
            registration.SessionBindings[session] = newBinding;

            if (!_sessionBindings.TryGetValue(session, out var groupBindings))
            {
                groupBindings = new Dictionary<uint, SessionBinding>();
                _sessionBindings[session] = groupBindings;
            }

            groupBindings[group] = newBinding;

            newBinding.OnUserIdAssigned(clientId =>
            {
                if (!registration.SessionBindings.TryGetValue(session, out var current) || !ReferenceEquals(current, newBinding))
                {
                    registration.PendingConnections.Remove(clientId);
                    return;
                }

                registration.BindingsByClientId[clientId] = newBinding;

                if (registration.PendingConnections.TryGetValue(clientId, out var pendingSession))
                {
                    registration.PendingConnections.Remove(clientId);
                    _BindSession(newBinding, pendingSession);
                }
                else
                {
                    _TryBindImmediate(newBinding);
                }
            });

            var joinValue = registration.Service.Join();

            void OnJoined(uint clientId)
            {
                lock (_syncRoot)
                {
                    joinValue.OnValue -= OnJoined;

                    if (!registration.SessionBindings.TryGetValue(session, out var current) || !ReferenceEquals(current, newBinding))
                    {
                        newBinding.SetUserId(clientId);
                        return;
                    }

                    newBinding.SetUserId(clientId);
                }
            }

            joinValue.OnValue += OnJoined;

            binding = newBinding;
            return true;
        }

        private void _TryBindImmediate(SessionBinding binding)
        {
            foreach (var candidate in binding.Registration.Service.ClientNotifier.Collection)
            {
                if (candidate.Id.Value == binding.UserId)
                {
                    _BindSession(binding, candidate);
                    break;
                }
            }
        }

        private void _BindSession(SessionBinding binding, IClientConnection clientConnection)
        {
            binding.ClientConnection = clientConnection;
            var success = binding.Session.Set(binding.Group, clientConnection);
            if (!success)
            {
                binding.ClientConnection = null;
                binding.Registration.Service.Leave(binding.UserId).GetAwaiter().GetResult();
                binding.Registration.BindingsByClientId.Remove(binding.UserId);
                binding.Registration.SessionBindings.Remove(binding.Session);

                if (_sessionBindings.TryGetValue(binding.Session, out var groupBindings))
                {
                    groupBindings.Remove(binding.Group);
                }

                _EnsureSessionBinding(binding.Session, binding.Group);
                return;
            }

            binding.Bound = true;
        }

        private void _ReleaseBinding(SessionBinding binding, bool reassign)
        {
            binding.Registration.SessionBindings.Remove(binding.Session);

            if (_sessionBindings.TryGetValue(binding.Session, out var groupBindings) && groupBindings.TryGetValue(binding.Group, out var current) && ReferenceEquals(current, binding))
            {
                groupBindings.Remove(binding.Group);
            }

            if (binding.Bound)
            {
                binding.Releasing = true;
                binding.Session.Unset(binding.Group);
            }

            binding.OnUserIdAssigned(clientId =>
            {
                binding.Registration.BindingsByClientId.Remove(clientId);
                binding.Registration.PendingConnections.Remove(clientId);
                binding.Registration.Service.Leave(clientId).GetAwaiter().GetResult();
            });

            if (reassign && _sessions.Contains(binding.Session))
            {
                _EnsureSessionBinding(binding.Session, binding.Group);
            }
        }

        private void _OnClientConnectionSupplied(IGameLobby service, IClientConnection clientConnection)
        {
            lock (_syncRoot)
            {
                if (!_registrationsByService.TryGetValue(service, out var registration))
                {
                    return;
                }

                if (!registration.BindingsByClientId.TryGetValue(clientConnection.Id.Value, out var binding))
                {
                    registration.PendingConnections[clientConnection.Id.Value] = clientConnection;
                    return;
                }

                if (binding.Bound)
                {
                    binding.ClientConnection = clientConnection;
                    return;
                }

                _BindSession(binding, clientConnection);
            }
        }

        private void _OnClientConnectionUnsupplied(IGameLobby service, IClientConnection clientConnection)
        {
            lock (_syncRoot)
            {
                if (!_registrationsByService.TryGetValue(service, out var registration))
                {
                    return;
                }

                if (!registration.BindingsByClientId.TryGetValue(clientConnection.Id.Value, out var binding))
                {
                    registration.PendingConnections.Remove(clientConnection.Id.Value);
                    return;
                }

                registration.BindingsByClientId.Remove(clientConnection.Id.Value);
                registration.SessionBindings.Remove(binding.Session);

                if (_sessionBindings.TryGetValue(binding.Session, out var groupBindings) && groupBindings.TryGetValue(registration.Group, out var current) && ReferenceEquals(current, binding))
                {
                    groupBindings.Remove(registration.Group);
                }

                if (binding.Bound && !binding.Releasing)
                {
                    binding.Session.Unset(registration.Group);
                }

                if (!binding.Releasing && _sessions.Contains(binding.Session))
                {
                    _EnsureSessionBinding(binding.Session, registration.Group);
                }
            }
        }
    }
}


