using System.Threading;
using PinionCore.Remote;
namespace PinionCore.Network
{
    public class ReverseStream : Network.IStreamable
    {
        private readonly Stream _Peer;

        public ReverseStream(Stream peer)
        {
            this._Peer = peer;
        }


        IAwaitableSource<int> IStreamable.Receive(byte[] buffer, int offset, int count, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return 0.ToWaitableValue();
            return _Peer.Pop(buffer, offset, count, token);

        }

        IAwaitableSource<int> IStreamable.Send(byte[] buffer, int offset, int count, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return 0.ToWaitableValue();
            return _Peer.Push(buffer, offset, count, token);
        }

        void System.IDisposable.Dispose()
        {
            // ReverseStream 不擁有 _Peer 的所有權，由創建者負責釋放
        }
    }
}
