using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using PinionCore.Remote;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Registrys
{
    public class ProviderRegistry : IDisposable
    {
        private readonly IProtocol _protocol;
        private readonly Dictionary<uint, ProviderSession> _sessions = new Dictionary<uint, ProviderSession>();
        private readonly object _sync = new object();
        private bool _disposed;

        public event Action<uint, IConnectionProvider> ProviderAddedEvent;
        public event Action<uint, IConnectionProvider> ProviderRemovedEvent;

        public ProviderRegistry()
        {
            _protocol = ProtocolProvider.Create();
        }

        public void Connect(uint id, EndPoint endPoint)
        {
            ProviderSession session;

            lock (_sync)
            {
                _EnsureNotDisposed();

                if (_sessions.ContainsKey(id))
                {
                    throw new InvalidOperationException($"Connection provider '{id}' already registered.");
                }
            }

            var set = PinionCore.Remote.Client.Provider.CreateTcpAgent(_protocol);
            PinionCore.Network.Tcp.Peer peer;

            try
            {
                peer = set.Connector.Connect(endPoint).GetAwaiter().GetResult();
            }
            catch
            {
                try
                {
                    set.Connector.Disconnect().GetAwaiter().GetResult();
                }
                catch
                {
                }

                throw;
            }

            session = new ProviderSession(this, id, set, peer);

            lock (_sync)
            {
                if (_disposed)
                {
                    session.Dispose();
                    throw new ObjectDisposedException(nameof(ProviderRegistry));
                }

                if (_sessions.ContainsKey(id))
                {
                    session.Dispose();
                    throw new InvalidOperationException($"Connection provider '{id}' already registered.");
                }

                _sessions.Add(id, session);
            }

            session.Start();
        }

        private void RemoveSession(uint id, ProviderSession session)
        {
            lock (_sync)
            {
                if (_sessions.TryGetValue(id, out ProviderSession current) && ReferenceEquals(current, session))
                {
                    _sessions.Remove(id);
                }
            }
        }

        private void NotifyAdded(uint id, IConnectionProvider provider)
        {
            ProviderAddedEvent?.Invoke(id, provider);
        }

        private void NotifyRemoved(uint id, IConnectionProvider provider)
        {
            ProviderRemovedEvent?.Invoke(id, provider);
        }

        public void Dispose()
        {
            ProviderSession[] sessions;

            lock (_sync)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                sessions = new ProviderSession[_sessions.Count];
                _sessions.Values.CopyTo(sessions, 0);
                _sessions.Clear();
            }

            foreach (ProviderSession session in sessions)
            {
                session?.Dispose();
            }
        }

        private void _EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ProviderRegistry));
            }
        }

        private sealed class ProviderSession : IDisposable
        {
            private readonly ProviderRegistry _owner;
            private readonly uint _id;
            private readonly PinionCore.Remote.Client.TcpConnectSet _set;
            private readonly PinionCore.Network.Tcp.Peer _peer;
            private readonly CancellationTokenSource _cts = new CancellationTokenSource();
            private readonly object _sync = new object();
            private IConnectionProvider _currentProvider;
            private Task _updateTask;
            private INotifier<IConnectionProvider> _notifier;
            private int _disposeState; // 0 = active, 1 = disposing, 2 = disposed
            private bool _started;

            public ProviderSession(ProviderRegistry owner, uint id, PinionCore.Remote.Client.TcpConnectSet set, PinionCore.Network.Tcp.Peer peer)
            {
                _owner = owner;
                _id = id;
                _set = set;
                _peer = peer;
            }

            public void Start()
            {
                lock (_sync)
                {
                    if (_started || _disposeState != 0)
                    {
                        return;
                    }

                    _notifier = _set.Agent.QueryNotifier<IConnectionProvider>();
                    _notifier.Supply += _OnSupply;
                    _notifier.Unsupply += _OnUnsupply;

                    _peer.BreakEvent += _OnBreak;
                    _peer.SocketErrorEvent += _OnSocketError;

                    _set.Agent.Enable(_peer);

                    _updateTask = Task.Run(() => _RunLoop(_cts.Token));
                    _started = true;
                }
            }

            public void Dispose()
            {
                if (Interlocked.CompareExchange(ref _disposeState, 1, 0) != 0)
                {
                    SpinWait.SpinUntil(() => System.Threading.Volatile.Read(ref _disposeState) == 2);
                    return;
                }

                _DisposeCore();
            }

            private void _RunLoop(CancellationToken token)
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        _set.Agent.HandleMessage();
                        _set.Agent.HandlePackets();

                        token.WaitHandle.WaitOne(1);
                    }
                }
                catch (Exception)
                {
                    _TriggerDispose();
                }
            }

            private void _OnSupply(IConnectionProvider provider)
            {
                lock (_sync)
                {
                    if (_disposeState != 0)
                    {
                        return;
                    }

                    _currentProvider = provider;
                }

                _owner.NotifyAdded(_id, provider);
            }

            private void _OnUnsupply(IConnectionProvider provider)
            {
                bool notify = false;

                lock (_sync)
                {
                    if (_disposeState != 0)
                    {
                        return;
                    }

                    if (ReferenceEquals(_currentProvider, provider))
                    {
                        _currentProvider = null;
                        notify = true;
                    }
                }

                if (notify)
                {
                    _owner.NotifyRemoved(_id, provider);
                }
            }

            private void _OnBreak()
            {
                _TriggerDispose();
            }

            private void _OnSocketError(SocketError _)
            {
                _TriggerDispose();
            }

            private void _TriggerDispose()
            {
                if (Interlocked.CompareExchange(ref _disposeState, 1, 0) == 0)
                {
                    Task.Run(_DisposeCore);
                }
            }

            private void _DisposeCore()
            {
                IConnectionProvider provider = null;

                try
                {
                    _cts.Cancel();

                    if (_notifier != null)
                    {
                        _notifier.Supply -= _OnSupply;
                        _notifier.Unsupply -= _OnUnsupply;
                    }

                    _peer.BreakEvent -= _OnBreak;
                    _peer.SocketErrorEvent -= _OnSocketError;

                    Task updateTask = _updateTask;
                    if (updateTask != null)
                    {
                        try
                        {
                            updateTask.Wait();
                        }
                        catch
                        {
                        }
                    }

                    lock (_sync)
                    {
                        provider = _currentProvider;
                        _currentProvider = null;
                    }

                    _owner.RemoveSession(_id, this);

                    if (provider != null)
                    {
                        _owner.NotifyRemoved(_id, provider);
                    }

                    if (_started)
                    {
                        _set.Agent.Disable();
                    }

                    try
                    {
                        _set.Connector.Disconnect().GetAwaiter().GetResult();
                    }
                    catch
                    {
                    }
                }
                finally
                {
                    _cts.Dispose();
                    Interlocked.Exchange(ref _disposeState, 2);
                }
            }
        }
    }
}

