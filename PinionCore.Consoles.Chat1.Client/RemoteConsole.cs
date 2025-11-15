using System;
using System.Net;

namespace PinionCore.Consoles.Chat1.Client
{
    internal sealed class RemoteConsole : Console
    {
        private PinionCore.Remote.Client.Tcp.ConnectingEndpoint? _endpoint;
        private bool _connected;

        public RemoteConsole(PinionCore.Remote.Ghost.IAgent agent)
            : base(agent)
        {
        }

        public bool Connect(string ip, int port)
        {
            if (_connected)
            {
                System.Console.WriteLine("Already connected.");
                return true;
            }

            try
            {
                var endpoint = new PinionCore.Remote.Client.Tcp.ConnectingEndpoint(new IPEndPoint(IPAddress.Parse(ip), port));
                var connectable = (PinionCore.Remote.Client.IConnectingEndpoint)endpoint;
                var stream = connectable.ConnectAsync().GetAwaiter().GetResult();
                Agent.Enable(stream);
                _endpoint = endpoint;
                Command.Register("disconnect", DisconnectCommand);
                _connected = true;
                System.Console.WriteLine($"Connected to {endpoint.EndPoint}.");
                return true;
            }
            catch (FormatException)
            {
                System.Console.WriteLine("Invalid IP address.");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Connect failed: {ex.Message}");
            }

            return false;
        }

        protected override void _Launch()
        {
            Command.Register<string, int>("connect", ConnectCommand);
            Command.Register("ping", ShowPing);
            base._Launch();
        }

        protected override void _Shutdown()
        {
            if (_connected)
            {
                Disconnect();
            }

            Command.Unregister("connect");
            Command.Unregister("disconnect");
            Command.Unregister("ping");
            base._Shutdown();
        }

        private void ShowPing()
        {
            System.Console.WriteLine($"ping:{Agent.Ping}");
        }

        private void ConnectCommand(string ip, int port)
        {
            Connect(ip, port);
        }

        private void DisconnectCommand()
        {
            Disconnect();
        }

        private void Disconnect()
        {
            if (!_connected)
            {
                return;
            }

            Agent.Disable();
            if (_endpoint != null)
            {
                ((IDisposable)_endpoint).Dispose();
                _endpoint = null;
            }
            Command.Unregister("disconnect");
            _connected = false;
            System.Console.WriteLine("Disconnected.");
        }
    }
}
