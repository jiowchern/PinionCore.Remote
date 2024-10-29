using System;
using System.Net.Sockets;

namespace PinionCore.Network.Tcp
{
    using PinionCore.Remote;

    public class SockerTransactor
    {
        public delegate IAsyncResult OnStart(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError errorCode, AsyncCallback callback, object state);


        readonly OnStart _StartHandler;

        public delegate int OnEnd(IAsyncResult arg);
        readonly OnEnd _EndHandler;

        event Action<SocketError> _SocketErrorEvent;
        public event Action<SocketError> SocketErrorEvent
        {
            add
            {
                _SocketErrorEvent += value;
            }

            remove
            {
                _SocketErrorEvent -= value;
            }
        }

        public SockerTransactor(OnStart start, OnEnd end)
        {

            _StartHandler = start;
            _EndHandler = end;
            _SocketErrorEvent += (e) => { };
        }


        public IWaitableValue<int> Transact(byte[] readed_byte, int offset, int count)
        {
            SocketError error;
            IAsyncResult ar = _StartHandler(readed_byte, offset, count, SocketFlags.None, out error, _StartDone, null);

            if (error != SocketError.Success && error != SocketError.IOPending)
            {
                _SocketErrorEvent(error);
                return System.Threading.Tasks.Task.Delay(1000).ContinueWith(t =>
                {
                    return 0;
                }).ToWaitableValue();

            }

            return System.Threading.Tasks.Task<int>.Factory.FromAsync(ar, (a) => { return _EndHandler(a); }).ToWaitableValue();
        }
        private void _StartDone(IAsyncResult arg)
        {

        }
    }


}
