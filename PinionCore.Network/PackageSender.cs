using System;
using System.Threading.Tasks;
using PinionCore.Memorys;


namespace PinionCore.Network
{
    public class PackageSender : IDisposable
    {
        private readonly IStreamable _Stream;
        private readonly IPool _Pool;

        private bool _isSending = false;
        private readonly System.Collections.Generic.Queue<Memorys.Buffer> _sendQueue = new System.Collections.Generic.Queue<Memorys.Buffer>();

        public PackageSender(IStreamable stream, PinionCore.Memorys.IPool pool)
        {
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

            _sendQueue.Enqueue(sendBuffer);

            if (!_isSending)
            {
                _ = _ProcessSendQueueAsync();
            }
        }

        void IDisposable.Dispose()
        {
            // Unity WebGL 不能同步等待 Task，避免死鎖
            // 清理佇列中的 buffer
            while (_sendQueue.Count > 0)
            {
                var buffer = _sendQueue.Dequeue();
             //   buffer.Dispose();
            }
        }

        private async Task _ProcessSendQueueAsync()
        {
            // 防止重複進入（WebGL 單執行緒環境下已足夠）
            if (_isSending)
                return;

            _isSending = true;

            try
            {
                while (_sendQueue.Count > 0)
                {
                    var buffer = _sendQueue.Dequeue();
                    try
                    {
                        await _SendBufferAsync(buffer);
                    }
                    finally
                    {
                        // 發送完成後釋放 buffer 回 pool
                        //buffer.Dispose();
                    }
                }
            }
            finally
            {
                _isSending = false;
            }
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

        
    }
}
