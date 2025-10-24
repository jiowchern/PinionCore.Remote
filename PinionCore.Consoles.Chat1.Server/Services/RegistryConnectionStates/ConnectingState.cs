using System;
using System.Net;
using System.Threading.Tasks;
using PinionCore.Utility;
using PinionCore.Network;

namespace PinionCore.Consoles.Chat1.Server.Services.RegistryConnectionStates
{
    /// <summary>
    /// 連接中狀態
    /// </summary>
    internal class ConnectingState : IStatus
    {
        private readonly PinionCore.Remote.Gateway.Registry _registry;
        private readonly string _routerHost;
        private readonly int _routerPort;
        private readonly PinionCore.Utility.Log _log;
        private bool _connectAttempted = false;

        public event Action OnConnected;
        public event Action OnConnectFailed;

        public ConnectingState(
            PinionCore.Remote.Gateway.Registry registry,
            string routerHost,
            int routerPort,
            PinionCore.Utility.Log log)
        {
            _registry = registry;
            _routerHost = routerHost;
            _routerPort = routerPort;
            _log = log;
        }

        void IStatus.Enter()
        {
            _log.WriteInfo(() => $"Registry 狀態: 連接中 ({_routerHost}:{_routerPort})");
        }

        void IStatus.Update()
        {
            if (!_connectAttempted)
            {
                _connectAttempted = true;
                _ = ConnectAsync();  // Fire and forget
            }
        }

        private async Task ConnectAsync()
        {
            try
            {
                var connector = new PinionCore.Network.Tcp.Connector();
                var endpoint = new IPEndPoint(IPAddress.Parse(_routerHost), _routerPort);
                var peer = await connector.Connect(endpoint);

                _registry.Agent.Enable(peer);

                _log.WriteInfo("成功連接到 Router");
                OnConnected?.Invoke();
            }
            catch (Exception ex)
            {
                _log.WriteInfo(() => $"連接到 Router 失敗: {ex.Message}");
                OnConnectFailed?.Invoke();
            }
        }

        void IStatus.Leave()
        {
        }
    }
}
