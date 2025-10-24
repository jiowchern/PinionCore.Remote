using System;
using PinionCore.Consoles.Chat1.Server.Services.RegistryConnectionStates;
using PinionCore.Network.Tcp;
using PinionCore.Utility;

namespace PinionCore.Consoles.Chat1.Server.Services
{
    /// <summary>
    /// Registry 連線管理器，使用 StatusMachine 管理連線狀態
    /// </summary>
    public class RegistryConnectionManager : IDisposable
    {
        private readonly PinionCore.Remote.Gateway.Registry _registry;
        private readonly string _routerHost;
        private readonly int _routerPort;
        private readonly PinionCore.Utility.Log _log;
        private readonly PinionCore.Utility.StatusMachine _machine;
        private readonly ExponentialBackoffReconnector _reconnector;
        private bool _disposed = false;

        public RegistryConnectionManager(
            PinionCore.Remote.Gateway.Registry registry,
            string routerHost,
            int routerPort,
            PinionCore.Utility.Log log)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _routerHost = routerHost ?? throw new ArgumentNullException(nameof(routerHost));
            _routerPort = routerPort;
            _log = log ?? throw new ArgumentNullException(nameof(log));

            _machine = new PinionCore.Utility.StatusMachine();
            _reconnector = new ExponentialBackoffReconnector();
        }

        /// <summary>
        /// 啟動連線流程
        /// </summary>
        public void Start()
        {
            // 開始於 Disconnected 狀態
            var disconnectedState = new DisconnectedState(_log);
            disconnectedState.OnStartConnect += TransitionToConnecting;
            _machine.Push(disconnectedState);
        }

        /// <summary>
        /// 更新狀態機
        /// </summary>
        public void Update()
        {
            _machine.Update();
        }

        /// <summary>
        /// 停止連線
        /// </summary>
        public void Stop()
        {
            _machine.Termination();
        }

        private void TransitionToConnecting()
        {
            var connectingState = new ConnectingState(_registry, _routerHost, _routerPort, _log);
            connectingState.OnConnected += (peer) => TransitionToConnected(peer);
            connectingState.OnConnectFailed += () => TransitionToReconnecting();
            _machine.Push(connectingState);
        }

        private void TransitionToConnected(Peer peer)
        {
            _reconnector.ResetRetryCount();  // 連接成功，重置重試計數
            var connectedState = new ConnectedState(_registry, peer, _log);
            connectedState.OnDisconnected += () => TransitionToReconnecting();
            _machine.Push(connectedState);
        }

        private void TransitionToReconnecting()
        {
            _reconnector.IncrementRetryCount();  // 增加重試計數
            var reconnectingState = new ReconnectingState(_reconnector, _log);
            reconnectingState.OnRetryConnect += () => TransitionToConnecting();
            _machine.Push(reconnectingState);
        }

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _machine.Termination();
            }
            catch (Exception ex)
            {
                _log?.WriteInfo(() => $"RegistryConnectionManager Dispose 錯誤: {ex.Message}");
            }

            _disposed = true;
        }
    }
}
