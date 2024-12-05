using System.Buffers;
using System.Collections.Concurrent;
using System.Threading;
using PinionCore.Remote;
namespace PinionCore.Network
{
    public class Stream : Network.IStreamable
    {
        readonly BufferRelay _Send;
        readonly BufferRelay _Receive;
        public Stream()
        {
            _Send = new BufferRelay();
            _Receive = new BufferRelay();
        }

        public IWaitableValue<int> Push(byte[] buffer, int offset, int count)
        {
            return _Receive.Push(buffer, offset, count);
        }

        public IWaitableValue<int> Pop(byte[] buffer, int offset, int count)
        {

            return _Send.Pop(buffer, offset, count);
        }

        IWaitableValue<int> IStreamable.Send(byte[] buffer, int offset, int count)
        {
            return _Send.Push(buffer, offset, count);
        }
        
      

        IWaitableValue<int> IStreamable.Receive(byte[] buffer, int offset, int count)
        {

            return _Receive.Pop(buffer, offset, count);
        }

    }
}
