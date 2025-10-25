using System;
using System.Net;
using System.Threading.Tasks;
using PinionCore.Remote.Gateway;

using PinionCore.Remote.Ghost;

namespace PinionCore.Consoles.Chat1.Client
{
    /// <summary>
    /// Gateway Router 模式的 Chat Client Console
    /// 透過 PinionCore.Remote.Gateway.Agent 連接到 Router
    /// </summary>
    internal sealed class GatewayConsole : Console
    {
        private readonly PinionCore.Network.Tcp.Connector _connector;
        private readonly PinionCore.Remote.Gateway.Agent _RouterAgent;
        IAgent _Agent => _RouterAgent;
        private bool _connected;


        public GatewayConsole(PinionCore.Remote.Gateway.Agent agent)
            : base(agent)
        {
            _connector = new PinionCore.Network.Tcp.Connector();
            _RouterAgent = agent;
        }

        public bool Connect(string routerHost, int routerPort)
        {
            if (_connected)
            {
                System.Console.WriteLine("Already connected to Router.");
                return true;
            }

            try
            {
                System.Console.WriteLine($"Connecting to Router at {routerHost}:{routerPort}...");
                var endpoint = new IPEndPoint(IPAddress.Parse(routerHost), routerPort);
                var peer = _connector.Connect(endpoint).GetAwaiter().GetResult();

                Agent.Enable(peer);
                _connected = true;

                System.Console.WriteLine($"Connected to Router. Waiting for routing allocation...");
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
                System.Console.WriteLine($"錯誤: 連接失敗 - {ex.Message}");
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
            _connector.Disconnect().GetAwaiter().GetResult();
            _connected = false;
            System.Console.WriteLine("Disconnected from Router.");
        }
    }
}
