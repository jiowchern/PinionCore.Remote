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
        private sealed class LobbyState
        {
            private readonly Queue<Value<IClientConnection>> _pendingRequests = new Queue<Value<IClientConnection>>();
            private readonly HashSet<IClientConnection> _leasedClients = new HashSet<IClientConnection>();
            private readonly ClientConnectionDisposer _owner;
            private readonly object _sync = new object();

            public LobbyState(ClientConnectionDisposer owner, IGameLobby lobby)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
                Lobby = lobby ?? throw new ArgumentNullException(nameof(lobby));
            }

            public IGameLobby Lobby { get; }
            public Action<IClientConnection> SupplyHandler => OnClientSupplied;

            public void Enqueue(Value<IClientConnection> request)
            {
                if (request == null)
                {
                    throw new ArgumentNullException(nameof(request));
                }

                lock (_sync)
                {
                    _pendingRequests.Enqueue(request);
                }
            }

            public IReadOnlyCollection<IClientConnection> DrainLeased()
            {
                lock (_sync)
                {
                    var snapshot = _leasedClients.ToArray();
                    _leasedClients.Clear();
                    return snapshot;
                }
            }

            public IReadOnlyCollection<Value<IClientConnection>> DrainPending()
            {
                lock (_sync)
                {
                    if (_pendingRequests.Count == 0)
                    {
                        return Array.Empty<Value<IClientConnection>>();
                    }

                    var snapshot = _pendingRequests.ToArray();
                    _pendingRequests.Clear();
                    return snapshot;
                }
            }

            public void ReleaseLease(IClientConnection client)
            {
                lock (_sync)
                {
                    _leasedClients.Remove(client);
                }
            }

            private void OnClientSupplied(IClientConnection client)
            {
                if (client == null)
                {
                    return;
                }

                Value<IClientConnection> pending = null;

                lock (_sync)
                {
                    if (_pendingRequests.Count > 0)
                    {
                        pending = _pendingRequests.Dequeue();
                        _leasedClients.Add(client);
                    }
                }

                if (pending != null)
                {
                    _owner.RegisterLease(client, this);
                    pending.SetValue(client);
                }
                else
                {
                    // No consumer waiting for the connection. Release it immediately so we don't retain it.
                    Lobby.Leave(client.Id.Value);
                }
            }
        }

        private readonly object _sync = new object();
        private readonly IGameLobbySelectionStrategy _selectionStrategy;
        private readonly List<IGameLobby> _lobbies = new List<IGameLobby>();
        private readonly Dictionary<IGameLobby, LobbyState> _lobbyStates = new Dictionary<IGameLobby, LobbyState>();
        private readonly ConcurrentDictionary<IClientConnection, LobbyState> _clientToLobby = new ConcurrentDictionary<IClientConnection, LobbyState>();
        private bool _disposed;

        public event Action<IClientConnection> ClientReleasedEvent;

        public ClientConnectionDisposer(IGameLobbySelectionStrategy selectionStrategy)
        {
            _selectionStrategy = selectionStrategy ?? throw new ArgumentNullException(nameof(selectionStrategy));
        }

        public void Add(IGameLobby info)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            lock (_sync)
            {
                ThrowIfDisposed();
                if (_lobbyStates.ContainsKey(info))
                {
                    return;
                }

                var state = new LobbyState(this, info);
                _lobbyStates.Add(info, state);
                _lobbies.Add(info);
                info.ClientNotifier.Base.Supply += state.SupplyHandler;
            }
        }

        public void Remove(IGameLobby lobby)
        {
            if (lobby == null)
            {
                throw new ArgumentNullException(nameof(lobby));
            }

            LobbyState state;
            lock (_sync)
            {
                if (!_lobbyStates.TryGetValue(lobby, out state))
                {
                    return;
                }

                _lobbyStates.Remove(lobby);
                _lobbies.Remove(lobby);
                lobby.ClientNotifier.Base.Supply -= state.SupplyHandler;
            }

            ReleaseLobby(state);
        }

        public Value<IClientConnection> Require()
        {
            ThrowIfDisposed();

            LobbyState lobbyState = null;
            lock (_sync)
            {
                if (_lobbies.Count == 0)
                {
                    throw new InvalidOperationException("No game lobby registered.");
                }

                var ordered = _selectionStrategy.OrderLobbies(_lobbies);
                lobbyState = ordered.Select(l => _lobbyStates.TryGetValue(l, out var state) ? state : null)
                    .FirstOrDefault(s => s != null);
            }

            if (lobbyState == null)
            {
                throw new InvalidOperationException("No available lobby could be selected.");
            }

            var request = new Value<IClientConnection>();
            lobbyState.Enqueue(request);
            lobbyState.Lobby.Join();
            return request;
        }

        public void Return(IClientConnection client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (!_clientToLobby.TryRemove(client, out var lobbyState))
            {
                return;
            }

            lobbyState.ReleaseLease(client);
            lobbyState.Lobby.Leave(client.Id.Value);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            List<LobbyState> states;
            lock (_sync)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                states = _lobbyStates.Values.ToList();
                foreach (var pair in _lobbyStates)
                {
                    pair.Key.ClientNotifier.Base.Supply -= pair.Value.SupplyHandler;
                }

                _lobbyStates.Clear();
                _lobbies.Clear();
            }

            foreach (var state in states)
            {
                ReleaseLobby(state);
            }
        }

        private void RegisterLease(IClientConnection client, LobbyState state)
        {
            _clientToLobby[client] = state;
        }

        private void ReleaseLobby(LobbyState state)
        {
            List<Exception> exceptions = null;

            var leased = state.DrainLeased();
            foreach (var client in leased)
            {
                _clientToLobby.TryRemove(client, out _);
                state.Lobby.Leave(client.Id.Value);

                var handler = ClientReleasedEvent;
                if (handler == null)
                {
                    continue;
                }

                try
                {
                    handler(client);
                }
                catch (Exception ex)
                {
                    if (exceptions == null)
                    {
                        exceptions = new List<Exception>();
                    }

                    exceptions.Add(ex);
                }
            }

            var pending = state.DrainPending();
            foreach (var request in pending)
            {
                request.SetValue(null);
            }

            if (exceptions != null)
            {
                throw new AggregateException("One or more ClientReleasedEvent handlers threw exceptions.", exceptions);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ClientConnectionDisposer));
            }
        }

        internal bool IsEmpty()
        {
            lock (_sync)
            {
                return _lobbyStates.Count == 0;
            }
        }
    }
}

