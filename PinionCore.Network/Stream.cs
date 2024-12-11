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

        public IWaitableValue<int> Push(byte[] buffer, int offset, int count)
        {
            return Receive.Push(buffer, offset, count);
        }

        public IWaitableValue<int> Pop(byte[] buffer, int offset, int count)
        {

            return Send.Pop(buffer, offset, count);
        }

        IWaitableValue<int> IStreamable.Send(byte[] buffer, int offset, int count)
        {
            return Send.Push(buffer, offset, count);
        }



        IWaitableValue<int> IStreamable.Receive(byte[] buffer, int offset, int count)
        {

            return Receive.Pop(buffer, offset, count);
        }

    }
}
