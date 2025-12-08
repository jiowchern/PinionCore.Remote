using System.Threading;
using PinionCore.Remote;
namespace PinionCore.Network
{
    public class Stream : Network.IStreamable
    {
        public readonly BufferRelay Send;
        public readonly BufferRelay Receive;
        public Stream()
        {
            Send = new BufferRelay();
            Receive = new BufferRelay();
        }

        public IAwaitableSource<int> Push(byte[] buffer, int offset, int count, CancellationToken cancellation = default)
        {
            return Receive.Push(buffer, offset, count, cancellation);
        }

        public IAwaitableSource<int> Pop(byte[] buffer, int offset, int count, CancellationToken cancellation = default)
        {

            return Send.Pop(buffer, offset, count, cancellation);
        }

        IAwaitableSource<int> IStreamable.Send(byte[] buffer, int offset, int count, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return 0.ToWaitableValue();
            return Send.Push(buffer, offset, count, token);
        }



        IAwaitableSource<int> IStreamable.Receive(byte[] buffer, int offset, int count, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return 0.ToWaitableValue();
            return Receive.Pop(buffer, offset, count, token);
        }

        void System.IDisposable.Dispose()
        {
            System.IDisposable sendDispose = Send;
            sendDispose.Dispose();

            System.IDisposable receiveDispose = Receive;
            receiveDispose.Dispose();
        }

    }
}
