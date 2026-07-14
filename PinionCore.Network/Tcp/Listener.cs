using System;
using System.Net;
using System.Net.Sockets;
using PinionCore.Utility;

namespace PinionCore.Network.Tcp
{
    public class Listener
    {
        private readonly System.Net.Sockets.Socket _Socket;
        private volatile bool _Closed;
        private event Action<Peer> _AcceptEvent;

        public event Action<Peer> AcceptEvent
        {
            add { _AcceptEvent += value; }
            remove { _AcceptEvent -= value; }
        }
        public Listener()
        {
            _Socket = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _Socket.NoDelay = true;
        }
        
        public Exception Bind(int Port, int backlog)
        {
            try 
            {
                _Socket.Bind(new IPEndPoint(IPAddress.Any, Port));
                _Socket.Listen(backlog);
            }            
            catch (Exception e)
            {
                Singleton<Log>.Instance.WriteInfo($"bind {e.ToString()}.");
                return e;
            }

            _Socket.BeginAccept(_Accept, state: null);
            return null;
        }

        private void _Accept(IAsyncResult Ar)
        {
            if (_Closed)
                return;

            try
            {
                System.Net.Sockets.Socket socket = _Socket.EndAccept(Ar);
                _AcceptEvent(new Peer(socket));
            }
            catch (ObjectDisposedException)
            {
                // Close() 與 EndAccept 的競態:socket 只會在 Close() 被 dispose,屬預期關閉流程
                return;
            }
            catch (Exception e)
            {
                Singleton<Log>.Instance.WriteInfo($"accept {e.ToString()}.");
            }

            // 單一連線 accept 失敗不應終止監聽,繼續等下一個連線
            try
            {
                _Socket.BeginAccept(_Accept, state: null);
            }
            catch (ObjectDisposedException)
            {
                // EndAccept 成功後、重新掛 accept 前被 Close() 的競態,靜默結束
            }
        }

        public void Close()
        {
            _Closed = true;
            _Socket.Close();
        }
    }
}
