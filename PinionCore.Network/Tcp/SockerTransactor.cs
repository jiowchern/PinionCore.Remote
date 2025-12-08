using System;
using System.Net.Sockets;

namespace PinionCore.Network.Tcp
{
    using System.Threading;
    using System.Threading.Tasks;
    using PinionCore.Remote;

    public class SockerTransactor
    {
        public delegate IAsyncResult OnStart(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError errorCode, AsyncCallback callback, object state);


        readonly OnStart _StartHandler;

        public delegate int OnEnd(IAsyncResult arg, CancellationToken token);
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


        public IAwaitableSource<int> Transact(byte[] buffer, int offset, int count, CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return 0.ToWaitableValue();
            }

            SocketError error;
            IAsyncResult ar = _StartHandler(buffer, offset, count, SocketFlags.None, out error, _StartDone, null);

            if (error != SocketError.Success && error != SocketError.IOPending)
            {
                _SocketErrorEvent(error);
                return Task.Delay(1000).ContinueWith(t =>
                {
                    return 0;
                }).ToWaitableValue();

            }

            CancellationTokenRegistration registration = default;
            TaskCompletionSource<int> cancellationTcs = null;
            if (token.CanBeCanceled)
            {
                cancellationTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                registration = token.Register(() => cancellationTcs.TrySetResult(0));
                cancellationTcs.Task.ContinueWith(_ => registration.Dispose(), TaskScheduler.Default);
            }

            var opTask = Task<int>.Factory.FromAsync(ar, (a) => { return _EndHandler(a, token); });

            if (cancellationTcs == null)
            {
                return opTask.ToWaitableValue();
            }

            opTask.ContinueWith(t =>
            {
                registration.Dispose();
                if (t.IsFaulted)
                {
                    _ = t.Exception;
                }
            }, TaskScheduler.Default);

            var combined = Task.WhenAny(opTask, cancellationTcs.Task).Unwrap();
            return combined.ToWaitableValue();
        }
        private void _StartDone(IAsyncResult arg)
        {

        }
    }


}
