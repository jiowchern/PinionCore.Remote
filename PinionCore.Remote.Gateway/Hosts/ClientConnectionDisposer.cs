using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using PinionCore.Remote;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Hosts
{
    class ClientConnectionDisposer : IDisposable
    {
        private readonly IGameLobbySelectionStrategy _SelectionStrategy;
        private readonly object _syncRoot = new object();
        private readonly Dictionary<IGameLobby, LobbyRegistration> _registrations;
        private readonly List<IGameLobby> _lobbies;
        private readonly ConcurrentDictionary<IClientConnection, LobbyRegistration> _connectionOrigins;
        private bool _disposed;

        public event Action<IClientConnection> ClientReleasedEvent;

        public ClientConnectionDisposer(IGameLobbySelectionStrategy selectionStrategy)
        {
            _SelectionStrategy = selectionStrategy ?? throw new ArgumentNullException(nameof(selectionStrategy));
            _registrations = new Dictionary<IGameLobby, LobbyRegistration>();
            _lobbies = new List<IGameLobby>();
            _connectionOrigins = new ConcurrentDictionary<IClientConnection, LobbyRegistration>();
        }

        public void Add(IGameLobby info)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            lock (_syncRoot)
            {
                ThrowIfDisposed();
                if (_registrations.ContainsKey(info))
                {
                    return;
                }

                var registration = new LobbyRegistration(this, info);
                _registrations.Add(info, registration);
                _lobbies.Add(info);
            }
        }

        public void Remove(IGameLobby lobby)
        {
            if (lobby == null)
            {
                throw new ArgumentNullException(nameof(lobby));
            }

            LobbyRegistration registration;
            lock (_syncRoot)
            {
                if (!_registrations.TryGetValue(lobby, out registration))
                {
                    return;
                }

                _registrations.Remove(lobby);
                _lobbies.Remove(lobby);
            }

            registration.Dispose();
        }

        public Value<IClientConnection> Require()
        {
            LobbyRegistration registration;
            lock (_syncRoot)
            {
                ThrowIfDisposed();
                if (_lobbies.Count == 0)
                {
                    throw new InvalidOperationException("No lobby registered.");
                }

                registration = SelectRegistration();
            }

            if (registration == null)
            {
                throw new InvalidOperationException("Failed to select a lobby for acquiring a connection.");
            }

            return registration.RequireConnection();
        }

        public void Return(IClientConnection client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (!_connectionOrigins.TryGetValue(client, out var registration))
            {
                return;
            }

            var leaveResult = registration.Lobby.Leave(client.Id.Value);
            leaveResult.OnValue += _ => { };
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            List<LobbyRegistration> registrations;
            lock (_syncRoot)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                registrations = _registrations.Values.ToList();
                _registrations.Clear();
                _lobbies.Clear();
            }

            foreach (var registration in registrations)
            {
                registration.Dispose();
            }

            _connectionOrigins.Clear();
        }

        private LobbyRegistration SelectRegistration()
        {
            var ordered = _SelectionStrategy.OrderLobbies(_lobbies);
            if (ordered != null)
            {
                foreach (var lobby in ordered)
                {
                    if (lobby != null && _registrations.TryGetValue(lobby, out var registration))
                    {
                        return registration;
                    }
                }
            }

            return _registrations.Values.FirstOrDefault();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ClientConnectionDisposer));
            }
        }

        private void RegisterConnection(IClientConnection connection, LobbyRegistration registration)
        {
            _connectionOrigins[connection] = registration;
        }

        private void UnregisterConnection(IClientConnection connection)
        {
            _connectionOrigins.TryRemove(connection, out _);
        }

        private void NotifyConnectionReleased(IClientConnection connection)
        {
            ClientReleasedEvent?.Invoke(connection);
        }

        private sealed class LobbyRegistration : IDisposable
        {
            private readonly ClientConnectionDisposer _owner;
            private readonly object _gate = new object();
            private readonly Dictionary<uint, Value<IClientConnection>> _pending;
            private readonly Dictionary<uint, IClientConnection> _awaiting;
            private readonly HashSet<IClientConnection> _leased;
            private readonly Action<IClientConnection> _supplyHandler;
            private readonly Action<IClientConnection> _unsupplyHandler;
            private readonly INotifier<IClientConnection> _notifier;
            private bool _disposed;

            public LobbyRegistration(ClientConnectionDisposer owner, IGameLobby lobby)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
                Lobby = lobby ?? throw new ArgumentNullException(nameof(lobby));
                _pending = new Dictionary<uint, Value<IClientConnection>>();
                _awaiting = new Dictionary<uint, IClientConnection>();
                _leased = new HashSet<IClientConnection>();

                _supplyHandler = OnSupply;
                _unsupplyHandler = OnUnsupply;

                _notifier = Lobby.ClientNotifier?.Base ?? throw new InvalidOperationException("Lobby notifier does not expose a base notifier.");
                _notifier.Supply += _supplyHandler;
                _notifier.Unsupply += _unsupplyHandler;
            }

            public IGameLobby Lobby { get; }

            public Value<IClientConnection> RequireConnection()
            {
                var value = new Value<IClientConnection>();
                var join = Lobby.Join();
                join.OnValue += id =>
                {
                    IClientConnection connection;
                    lock (_gate)
                    {
                        if (_disposed)
                        {
                            value.SetValue(default);
                            return;
                        }

                        if (_awaiting.TryGetValue(id, out connection))
                        {
                            _awaiting.Remove(id);
                            _leased.Add(connection);
                        }
                        else
                        {
                            _pending[id] = value;
                            return;
                        }
                    }

                    _owner.RegisterConnection(connection, this);
                    value.SetValue(connection);
                };

                return value;
            }

            private void OnSupply(IClientConnection connection)
            {
                Value<IClientConnection> pending = null;
                var deliver = false;

                lock (_gate)
                {
                    if (_disposed)
                    {
                        return;
                    }

                    var id = connection.Id.Value;
                    if (_pending.TryGetValue(id, out pending))
                    {
                        _pending.Remove(id);
                        _leased.Add(connection);
                        deliver = true;
                    }
                    else
                    {
                        _awaiting[id] = connection;
                        return;
                    }
                }

                if (deliver && pending != null)
                {
                    _owner.RegisterConnection(connection, this);
                    pending.SetValue(connection);
                }
            }

            private void OnUnsupply(IClientConnection connection)
            {
                var id = connection.Id.Value;
                var wasLeased = false;

                lock (_gate)
                {
                    _awaiting.Remove(id);
                    _pending.Remove(id);
                    wasLeased = _leased.Remove(connection);
                }

                _owner.UnregisterConnection(connection);

                if (wasLeased)
                {
                    _owner.NotifyConnectionReleased(connection);
                }
            }

            private void ReleaseAllConnections()
            {
                List<IClientConnection> toRelease;
                lock (_gate)
                {
                    toRelease = new List<IClientConnection>(_leased);
                    toRelease.AddRange(_awaiting.Values);
                }

                foreach (var connection in toRelease.Distinct())
                {
                    var leave = Lobby.Leave(connection.Id.Value);
                    leave.OnValue += _ => { };
                }
            }

            public void Dispose()
            {
                List<Value<IClientConnection>> pendingValues;

                lock (_gate)
                {
                    if (_disposed)
                    {
                        return;
                    }

                    _disposed = true;
                    pendingValues = _pending.Values.ToList();
                }

                ReleaseAllConnections();

                _notifier.Supply -= _supplyHandler;
                _notifier.Unsupply -= _unsupplyHandler;

                foreach (var pending in pendingValues)
                {
                    pending.SetValue(default);
                }
            }
        }
    }
}
