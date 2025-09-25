using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PinionCore.Remote;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Hosts
{
    class ClientConnectionDisposer : IDisposable
    {
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

            lock (_syncRoot)
            {
                ThrowIfDisposed();

                if (!_lobbies.Contains(info))
                {
                    _lobbies.Add(info);
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

            lock (_syncRoot)
            {
                if (_disposed)
                {
                    return;
                }

                _lobbies.Remove(lobby);

                if (_connectionsByLobby.TryGetValue(lobby, out var connections) && connections.Count > 0)
                {
                    releasedConnections = connections.ToList();
                    _connectionsByLobby.Remove(lobby);

                    foreach (var connection in releasedConnections)
                    {
                        _connectionToLobby.Remove(connection);
                    }
                }
            }

            if (releasedConnections == null || releasedConnections.Count == 0)
            {
                return;
            }

            foreach (var connection in releasedConnections)
            {
                try
                {
                    ClientReleasedEvent?.Invoke(connection);
                }
                finally
                {
                    lobby.Leave(connection.Id.Value);
                }
            }
        }

        public Value<IClientConnection> Require()
        {
            IGameLobby selectedLobby = null;

            lock (_syncRoot)
            {
                ThrowIfDisposed();

                if (_lobbies.Count == 0)
                {
                    throw new InvalidOperationException("No game lobby available.");
                }

                var snapshot = _lobbies.ToList();
                selectedLobby = _selectionStrategy.OrderLobbies(snapshot).FirstOrDefault();
            }

            if (selectedLobby == null)
            {
                throw new InvalidOperationException("No suitable game lobby available.");
            }

            var result = new Value<IClientConnection>();

            _ = AcquireAsync(selectedLobby).ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    result.SetValue(task.Result);
                }
                else if (task.IsFaulted && task.Exception != null)
                {
                    throw task.Exception.InnerException ?? task.Exception;
                }
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

            return result;
        }

        public void Return(IClientConnection client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            IGameLobby lobby = null;

            lock (_syncRoot)
            {
                if (_connectionToLobby.TryGetValue(client, out lobby))
                {
                    _connectionToLobby.Remove(client);

                    if (_connectionsByLobby.TryGetValue(lobby, out var connections))
                    {
                        connections.Remove(client);

                        if (connections.Count == 0)
                        {
                            _connectionsByLobby.Remove(lobby);
                        }
                    }
                }
            }

            if (lobby != null)
            {
                lobby.Leave(client.Id.Value);
            }
        }

        public void Dispose()
        {
            List<(IGameLobby Lobby, IClientConnection Connection)> leases = null;

            lock (_syncRoot)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;

                leases = _connectionToLobby.Select(pair => (pair.Value, pair.Key)).ToList();

                _connectionToLobby.Clear();
                _connectionsByLobby.Clear();
                _lobbies.Clear();
            }

            foreach (var (lobby, connection) in leases)
            {
                try
                {
                    ClientReleasedEvent?.Invoke(connection);
                }
                finally
                {
                    lobby.Leave(connection.Id.Value);
                }
            }
        }

        private readonly IGameLobbySelectionStrategy _selectionStrategy;
        private readonly List<IGameLobby> _lobbies = new List<IGameLobby>();
        private readonly Dictionary<IClientConnection, IGameLobby> _connectionToLobby = new Dictionary<IClientConnection, IGameLobby>();
        private readonly Dictionary<IGameLobby, HashSet<IClientConnection>> _connectionsByLobby = new Dictionary<IGameLobby, HashSet<IClientConnection>>();
        private readonly object _syncRoot = new object();
        private bool _disposed;

        private async Task<IClientConnection> AcquireAsync(IGameLobby lobby)
        {
            var clientId = await lobby.Join();
            var connection = await WaitForConnectionAsync(lobby, clientId);

            lock (_syncRoot)
            {
                if (!_connectionsByLobby.TryGetValue(lobby, out var connections))
                {
                    connections = new HashSet<IClientConnection>();
                    _connectionsByLobby[lobby] = connections;
                }

                connections.Add(connection);
                _connectionToLobby[connection] = lobby;
            }

            return connection;
        }

        private static Task<IClientConnection> WaitForConnectionAsync(IGameLobby lobby, uint clientId)
        {
            var tcs = new TaskCompletionSource<IClientConnection>(TaskCreationOptions.RunContinuationsAsynchronously);
            var notifier = lobby.ClientNotifier.Base;

            void Handler(IClientConnection connection)
            {
                if (connection.Id.Value == clientId && tcs.TrySetResult(connection))
                {
                    notifier.Supply -= Handler;
                }
            }

            notifier.Supply += Handler;

            return tcs.Task;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ClientConnectionDisposer));
            }
        }
    }
}
