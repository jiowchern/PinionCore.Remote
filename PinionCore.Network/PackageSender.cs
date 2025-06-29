using System;
using System.Threading.Tasks;
using PinionCore.Memorys;


namespace PinionCore.Network
{
    public class PackageSender : IDisposable
    {
        private readonly IStreamable _Stream;
        private readonly IPool _Pool;


        private Task<int> _Sending;

        public PackageSender(IStreamable stream, PinionCore.Memorys.IPool pool)
        {
            _Sending = Task.FromResult(0);

            _Stream = stream;
            _Pool = pool;
        }

        public void Push(PinionCore.Memorys.Buffer buffer)
        {
            if (buffer.Count == 0)
                return;

            var packageVarintCount = PinionCore.Serialization.Varint.GetByteCount(buffer.Bytes.Count);
            Memorys.Buffer sendBuffer = _Pool.Alloc(packageVarintCount + buffer.Bytes.Count);
            var offset = PinionCore.Serialization.Varint.NumberToBuffer(sendBuffer.Bytes.Array, sendBuffer.Bytes.Offset, buffer.Bytes.Count);

            for (var i = 0; i < buffer.Bytes.Count; i++)
            {
                sendBuffer.Bytes.Array[sendBuffer.Bytes.Offset + offset + i] = buffer.Bytes.Array[buffer.Bytes.Offset + i];
            }
            offset += buffer.Bytes.Count;

            _Push(sendBuffer);
        }

        void IDisposable.Dispose()
        {
            _Sending.Dispose();
        }



        private void _Push(Memorys.Buffer buffer)
        {

            _Sending = _Sending.ContinueWith(t => _SendBufferAsync(buffer)).Unwrap();
        }

        private async Task<int> _SendBufferAsync(PinionCore.Memorys.Buffer buffer)
        {
            var sendCount = 0;
            do
            {
                var count = await _Stream.Send(buffer.Bytes.Array, buffer.Bytes.Offset + sendCount, buffer.Count - sendCount);
                if (count == 0)
                    return 0;
                sendCount += count;

            } while (sendCount < buffer.Count);
            return sendCount;
        }

        public bool IsSending
        {
            get { return !_Sending.IsCompleted; }

        }
    }
}
