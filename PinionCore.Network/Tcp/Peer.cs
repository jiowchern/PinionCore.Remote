using System;
using System.Net.Sockets;
using System.Threading;

namespace PinionCore.Network.Tcp
{
    using PinionCore.Remote;
    public class Peer : IStreamable
    {
        public readonly System.Net.Sockets.Socket Socket;
        readonly SockerTransactor _Send;
        readonly SockerTransactor _Receive;
        public Peer(System.Net.Sockets.Socket socket)
        {

            Socket = socket;
            _Receive = new SockerTransactor(Socket.BeginReceive, _EndReceive);
            _Receive.SocketErrorEvent += (e) => { ReceiveEvent(0); };
            _Send = new SockerTransactor(Socket.BeginSend, _EndSend);
            _Send.SocketErrorEvent += (e) => { SendEvent(0); };

            _SocketErrorEvent += (e) => { BreakEvent(); };
            ReceiveEvent += (size) => { if (size == 0) BreakEvent(); };
            SendEvent += (size) => { if (size == 0) BreakEvent(); };
            BreakEvent += () => { };
        }

        public event System.Action BreakEvent;
        public event System.Action<int> ReceiveEvent;
        public event System.Action<int> SendEvent;
        event Action<SocketError> _SocketErrorEvent;
        public event Action<SocketError> SocketErrorEvent
        {
            add
            {
                _SocketErrorEvent += value;
                _Receive.SocketErrorEvent += value;
                _Send.SocketErrorEvent += value;
            }

            remove
            {
                _SocketErrorEvent -= value;
                _Receive.SocketErrorEvent -= value;
                _Send.SocketErrorEvent -= value;
            }
        }

        IAwaitableSource<int> IStreamable.Receive(byte[] buffer, int offset, int count, CancellationToken token)
        {            
            return _Receive.Transact(buffer, offset, count, token);
        }
        private int _EndReceive(IAsyncResult arg, CancellationToken token)
        {
            SocketError error;

            int size;
            try
            {
                size = Socket.EndReceive(arg, out error);
            }
            catch (ObjectDisposedException)
            {
                return 0;
            }
            catch (SocketException se)
            {
                _SocketErrorEvent(se.SocketErrorCode);
                return 0;
            }

            if (token.IsCancellationRequested)
                return 0;

            ReceiveEvent(size);
            if (error == SocketError.Success)
                return size;

            _SocketErrorEvent(error);
            return size;
        }
        IAwaitableSource<int> IStreamable.Send(byte[] buffer, int offset, int buffer_length, CancellationToken token)
        {         
            return _Send.Transact(buffer, offset, buffer_length, token);
        }

        private int _EndSend(IAsyncResult arg, CancellationToken token)
        {
            SocketError error;

            int size;
            try
            {
                size = Socket.EndSend(arg, out error);
            }
            catch (ObjectDisposedException)
            {
                return 0;
            }
            catch (SocketException se)
            {
                _SocketErrorEvent(se.SocketErrorCode);
                return 0;
            }

            if (token.IsCancellationRequested)
                return 0;
            SendEvent(size);
            if (error == SocketError.Success)
                return size;

            _SocketErrorEvent(error);
            return size;
        }
        protected System.Net.Sockets.Socket GetSocket()
        {
            return Socket;
        }

        void IDisposable.Dispose()
        {
            Socket.Close();
        }

        public System.Threading.Tasks.Task Disconnect(bool reuse = false)
        {
            if (Socket.Connected)
            {
                Socket.Shutdown(SocketShutdown.Both);
                return System.Threading.Tasks.Task.Factory.FromAsync(
                            (handler, obj) => Socket.BeginDisconnect(reuse, handler, null),
                            Socket.EndDisconnect,
                            null);

            }
            else
            {
                return System.Threading.Tasks.Task.CompletedTask;
            }
        }

    }


}
