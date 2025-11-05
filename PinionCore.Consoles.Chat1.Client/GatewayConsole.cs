using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading.Tasks;
using PinionCore.Remote.Gateway;
using PinionCore.Remote.Ghost;

namespace PinionCore.Consoles.Chat1.Client
{
    /// <summary>
    /// Gateway Router 模式的 Chat Client Console
    /// 透過 PinionCore.Remote.Gateway.Agent 連接到 Router
    /// 支援 TCP 和 WebSocket 兩種協議
    /// </summary>
    internal sealed class GatewayConsole : Console
    {
        private readonly PinionCore.Remote.Gateway.Agent _RouterAgent;
        private IAgent _Agent => _RouterAgent;
        private bool _connected;
        

        System.Action _Dispose;

        public GatewayConsole(PinionCore.Remote.Gateway.Agent agent)
            : base(agent)
        {
            _Dispose = () => { };
            _RouterAgent = agent;
        }

        /// <summary>
        /// 使用 TCP 連接到 Router
        /// </summary>
        public bool ConnectTcp(string routerHost, int routerPort)
        {
            if (_connected)
            {
                System.Console.WriteLine("Already connected to Router.");
                return true;
            }

            try
            {
                System.Console.WriteLine($"[TCP] Connecting to Router at {routerHost}:{routerPort}...");
                var tcpConnector = new PinionCore.Network.Tcp.Connector();
                var endpoint = new IPEndPoint(IPAddress.Parse(routerHost), routerPort);
                var peer = tcpConnector.Connect(endpoint).GetAwaiter().GetResult();

                

                Agent.Enable(peer);
                _Dispose = () => peer.Disconnect();
                
                _connected = true;

                System.Console.WriteLine($"[TCP] Connected to Router. Waiting for routing allocation...");
                return true;
            }
            catch (FormatException)
            {
                System.Console.WriteLine($"錯誤: 無效的 Router IP 位址: {routerHost}");
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                System.Console.WriteLine($"錯誤: 無法連接到 Router ({routerHost}:{routerPort}) - {ex.Message}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"錯誤: TCP 連接失敗 - {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// 使用 WebSocket 連接到 Router
        /// </summary>
        public bool ConnectWebSocket(string routerHost, int routerPort)
        {
            if (_connected)
            {
                System.Console.WriteLine("Already connected to Router.");
                return true;
            }

            try
            {
                System.Console.WriteLine($"[WebSocket] Connecting to Router at ws://{routerHost}:{routerPort}/...");
                var clientWebSocket = new ClientWebSocket();
                var webConnector = new PinionCore.Network.Web.Connecter(clientWebSocket);
                var address = $"ws://{routerHost}:{routerPort}/";

                var peer=  webConnector.ConnectAsync(address).GetAwaiter().GetResult();
                
                if (peer == null)
                {
                    System.Console.WriteLine($"錯誤: WebSocket 連接失敗");
                    return false;
                }

                Agent.Enable(peer);
                _Dispose = () => {
                    peer.DisconnectAsync().GetAwaiter().GetResult();
                    // Web.Connecter 繼承 Web.Peer，Peer 實作 IDisposable
                    ((IDisposable)webConnector).Dispose();
                };
                _connected = true;

                System.Console.WriteLine($"[WebSocket] Connected to Router. Waiting for routing allocation...");
                return true;
            }
            catch (WebSocketException ex)
            {
                System.Console.WriteLine($"錯誤: WebSocket 連接失敗 - {ex.Message}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"錯誤: WebSocket 連接失敗 - {ex.Message}");
            }

            return false;
        }

        protected override void _Launch()
        {
            base._Launch();
        }

        protected override void _Shutdown()
        {
            if (_connected)
            {
                Disconnect();
            }

            
            base._Shutdown();
        }

        protected override void _Update()
        {
            var ping = _Agent.Ping;
            _Agent.HandleMessage();
            _Agent.HandlePackets();




            base._Update();
        }

        private void Disconnect()
        {
            if (!_connected)
            {
                return;
            }

            Agent.Disable();           

            _connected = false;
            System.Console.WriteLine("Disconnected from Router.");
        }
    }
}
