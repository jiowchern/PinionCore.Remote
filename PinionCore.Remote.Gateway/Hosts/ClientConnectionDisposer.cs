using System;
using System.Collections.Generic;
using System.Linq;
using PinionCore.Remote;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Hosts
{
    class ClientConnectionDisposer : IDisposable
    {
        private readonly IGameLobbySelectionStrategy _selectionStrategy;
        private readonly List<LobbyState> _lobbies;
        private readonly Dictionary<IGameLobby, LobbyState> _lobbyLookup;
        private readonly Queue<PendingRequest> _pendingRequests;
        private readonly Dictionary<IClientConnection, LobbyState> _activeConnections;
        private readonly object _syncRoot;
        private bool _disposed;

        public event Action<IClientConnection> ClientReleasedEvent;

        public ClientConnectionDisposer(IGameLobbySelectionStrategy selectionStrategy)
        {
            _selectionStrategy = selectionStrategy ?? throw new ArgumentNullException(nameof(selectionStrategy));
            _lobbies = new List<LobbyState>();
            _lobbyLookup = new Dictionary<IGameLobby, LobbyState>();
            _pendingRequests = new Queue<PendingRequest>();
            _activeConnections = new Dictionary<IClientConnection, LobbyState>();
            _syncRoot = new object();
        }

        public void Add(IGameLobby info)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            LobbyState state;

            lock (_syncRoot)
            {
                ThrowIfDisposed();

                if (_lobbyLookup.ContainsKey(info))
                {
                    return;
                }

                state = new LobbyState(this, info);
                _lobbies.Add(state);
                _lobbyLookup.Add(info, state);

                foreach (var request in _pendingRequests)
                {
                    if (!request.JoinRequested)
                    {
                        IssueJoinForRequest(request);
                    }
                }
            }
        }

        public void Remove(IGameLobby lobby)
        {
            if (lobby == null)
            {
                throw new ArgumentNullException(nameof(lobby));
            }

            List<IClientConnection> releasedConnections = null;
            LobbyState state = null;

            lock (_syncRoot)
            {
                if (!_lobbyLookup.TryGetValue(lobby, out state))
                {
                    return;
                }

                _lobbies.Remove(state);
                _lobbyLookup.Remove(lobby);

                releasedConnections = state.DrainAvailable() ?? new List<IClientConnection>();

                foreach (var pair in _activeConnections.Where(p => ReferenceEquals(p.Value, state)).ToList())
                {
                    releasedConnections.Add(pair.Key);
                    _activeConnections.Remove(pair.Key);
                }

                state.Dispose();

                if (_lobbies.Count == 0)
                {
                    foreach (var request in _pendingRequests)
                    {
                        request.JoinRequested = false;
                    }
                }
            }

            if (state == null)
            {
                return;
            }

            foreach (var connection in releasedConnections)
            {
                try
                {
                    state.Lobby.Leave(connection.Id.Value);
                }
                catch
                {
                }

                ClientReleasedEvent?.Invoke(connection);
            }
        }

        public Value<IClientConnection> Require()
        {
            var value = new Value<IClientConnection>();
            LobbyState assignedState;
            IClientConnection connection;

            lock (_syncRoot)
            {
                ThrowIfDisposed();

                if (TryGetNextAvailableConnection(out assignedState, out connection))
                {
                    _activeConnections[connection] = assignedState;
                    value.SetValue(connection);
                    return value;
                }

                var pending = new PendingRequest(value);
                _pendingRequests.Enqueue(pending);

                if (_lobbies.Count > 0)
                {
                    IssueJoinForRequest(pending);
                }
            }

            return value;
        }

        public void Return(IClientConnection client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            LobbyState state;

            lock (_syncRoot)
            {
                if (!_activeConnections.TryGetValue(client, out state))
                {
                    return;
                }

                _activeConnections.Remove(client);
            }

            state.Lobby.Leave(client.Id.Value);
        }

        public void Dispose()
        {
            List<LobbyState> states;

            lock (_syncRoot)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                states = _lobbies.ToList();
            }

            foreach (var state in states)
            {
                Remove(state.Lobby);
            }
        }

        private void HandleClientSupplied(LobbyState state, IClientConnection connection)
        {
            LobbyState assignedState;
            IClientConnection assignConnection;

            lock (_syncRoot)
            {
                if (_disposed)
                {
                    return;
                }

                state.Enqueue(connection);

                while (_pendingRequests.Count > 0 && TryGetNextAvailableConnection(out assignedState, out assignConnection))
                {
                    var request = _pendingRequests.Dequeue();
                    _activeConnections[assignConnection] = assignedState;
                    request.Completion.SetValue(assignConnection);
                }
            }
        }

        private void HandleClientUnsupplied(LobbyState state, IClientConnection connection)
        {
            lock (_syncRoot)
            {
                state.Remove(connection);
                _activeConnections.Remove(connection);
            }
        }

        private void IssueJoinForRequest(PendingRequest request)
        {
            var lobbyState = GetOrderedLobbies().FirstOrDefault();
            if (lobbyState == null)
            {
                return;
            }

            request.JoinRequested = true;
            lobbyState.RequestJoin();
        }

        private bool TryGetNextAvailableConnection(out LobbyState state, out IClientConnection connection)
        {
            foreach (var lobbyState in GetOrderedLobbies())
            {
                if (lobbyState.TryDequeue(out connection))
                {
                    state = lobbyState;
                    return true;
                }
            }

            state = null;
            connection = null;
            return false;
        }

        private IEnumerable<LobbyState> GetOrderedLobbies()
        {
            var snapshot = _lobbies.Select(l => l.Lobby).ToList();
            if (snapshot.Count == 0)
            {
                yield break;
            }

            foreach (var lobby in _selectionStrategy.OrderLobbies(snapshot))
            {
                if (_lobbyLookup.TryGetValue(lobby, out var state))
                {
                    yield return state;
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ClientConnectionDisposer));
            }
        }

        private sealed class PendingRequest
        {
            public PendingRequest(Value<IClientConnection> completion)
            {
                Completion = completion;
            }

            public Value<IClientConnection> Completion { get; }

            public bool JoinRequested { get; set; }
        }

        private sealed class LobbyState : IDisposable
        {
            private readonly ClientConnectionDisposer _owner;
            private readonly Queue<IClientConnection> _availableConnections;
            private readonly Notifier<IClientConnection> _notifier;

            public LobbyState(ClientConnectionDisposer owner, IGameLobby lobby)
            {
                _owner = owner;
                Lobby = lobby;
                _availableConnections = new Queue<IClientConnection>();
                _notifier = lobby.ClientNotifier;
                _notifier.Base.Supply += OnSupply;
                _notifier.Base.Unsupply += OnUnsupply;
            }

            public IGameLobby Lobby { get; }

            public void RequestJoin()
            {
                Lobby.Join();
            }

            public void Enqueue(IClientConnection connection)
            {
                if (connection != null)
                {
                    _availableConnections.Enqueue(connection);
                }
            }

            public bool TryDequeue(out IClientConnection connection)
            {
                if (_availableConnections.Count > 0)
                {
                    connection = _availableConnections.Dequeue();
                    return true;
                }

                connection = null;
                return false;
            }

            public void Remove(IClientConnection connection)
            {
                if (_availableConnections.Count == 0 || connection == null)
                {
                    return;
                }

                var count = _availableConnections.Count;
                var retained = new Queue<IClientConnection>(count);

                while (_availableConnections.Count > 0)
                {
                    var current = _availableConnections.Dequeue();
                    if (!ReferenceEquals(current, connection))
                    {
                        retained.Enqueue(current);
                    }
                }

                while (retained.Count > 0)
                {
                    _availableConnections.Enqueue(retained.Dequeue());
                }
            }

            public List<IClientConnection> DrainAvailable()
            {
                var list = new List<IClientConnection>(_availableConnections);
                _availableConnections.Clear();
                return list;
            }

            public void Dispose()
            {
                _notifier.Base.Supply -= OnSupply;
                _notifier.Base.Unsupply -= OnUnsupply;
            }

            private void OnSupply(IClientConnection connection)
            {
                _owner.HandleClientSupplied(this, connection);
            }

            private void OnUnsupply(IClientConnection connection)
            {
                _owner.HandleClientUnsupplied(this, connection);
            }
        }
    }
}
