using System.Net;
using PinionCore.Network.Tcp;
using PinionCore.Utility;

namespace PinionCore.Consoles.Chat1.Client
{
    class ConnectState : IStatus
    {
        private const string ConnectGate = "connect-gate";
        private const string Connect = "connect";
        private Command _Command;
        Peer _Peer;
        public event System.Action<Peer> DoneEvent;
        public ConnectState(Command command)
        {
            _Command = command;
        }

        void IStatus.Enter()
        {
            _Command.Register<string>(ConnectGate, _ConnectGate);
            _Command.Register<string>(Connect, _Connect);
        }

        private async void _Connect(string ipendpoint)
        {
            _Command.Unregister(ConnectGate);
            _Command.Unregister(Connect);
            var ip = IPEndPoint.Parse(ipendpoint);
            var connector = new PinionCore.Network.Tcp.Connector();
            var peer = await connector.Connect(ip);
            if (peer == null)
            {
                _Command.Register<string>(ConnectGate, _ConnectGate);
                _Command.Register<string>(Connect, _Connect);
                return;
            }

            _Peer = peer;

        }

        private void _ConnectGate(string obj)
        {
            throw new NotImplementedException();
        }

        void IStatus.Leave()
        {
            
        }

        void IStatus.Update()
        {
            if (_Peer != null)
            {
                DoneEvent(_Peer);
            }
        }
    }
}
