using System;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace PinionCore.Network.Web
{
    public class Connecter 
    {

        readonly ClientWebSocket _Socket;

        public Connecter(ClientWebSocket socket) 
        {
            _Socket = socket;
        }

        public System.Threading.Tasks.Task<Peer> ConnectAsync(string address)
        {
            Task connectTask = _Socket.ConnectAsync(new Uri(address), System.Threading.CancellationToken.None);
            return connectTask.ContinueWith<Peer>(_ConnectResult);
        }


        private Peer _ConnectResult(Task arg)
        {
            if (_Socket.State == WebSocketState.Open)
            {
                return new Peer(_Socket);
            }
            else
            {
                return null;
            }
        }
    }
}
