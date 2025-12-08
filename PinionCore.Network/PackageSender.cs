using System;
using System.Threading;
using System.Threading.Tasks;
using PinionCore.Memorys;


namespace PinionCore.Network
{
    public class PackageSender : IDisposable
    {
        private readonly IStreamable _Stream;
        private readonly IPool _Pool;

        private bool _isSending = false;
        private readonly struct PendingSend
        {
            public PendingSend(Memorys.Buffer buffer, CancellationToken token)
            {
                Buffer = buffer;
                Token = token;
            }

            public Memorys.Buffer Buffer { get; }
            public CancellationToken Token { get; }
        }

        private readonly System.Collections.Generic.Queue<PendingSend> _sendQueue = new System.Collections.Generic.Queue<PendingSend>();

        public PackageSender(IStreamable stream, PinionCore.Memorys.IPool pool)
        {
            _Stream = stream;
            _Pool = pool;
        }

        public void Push(PinionCore.Memorys.Buffer buffer, CancellationToken cancellation = default)
        {
            if (buffer.Count == 0 || cancellation.IsCancellationRequested)
                return;

            var packageVarintCount = PinionCore.Serialization.Varint.GetByteCount(buffer.Bytes.Count);
            Memorys.Buffer sendBuffer = _Pool.Alloc(packageVarintCount + buffer.Bytes.Count);
            var offset = PinionCore.Serialization.Varint.NumberToBuffer(sendBuffer.Bytes.Array, sendBuffer.Bytes.Offset, buffer.Bytes.Count);

            for (var i = 0; i < buffer.Bytes.Count; i++)
            {
                sendBuffer.Bytes.Array[sendBuffer.Bytes.Offset + offset + i] = buffer.Bytes.Array[buffer.Bytes.Offset + i];
            }

            _sendQueue.Enqueue(new PendingSend(sendBuffer, cancellation));

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
                var pending = _sendQueue.Dequeue();
                var buffer = pending.Buffer;
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
                    var pending = _sendQueue.Dequeue();
                    if (pending.Token.IsCancellationRequested)
                    {
                        continue;
                    }
                    try
                    {
                        await _SendBufferAsync(pending.Buffer, pending.Token);
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

        private async Task<int> _SendBufferAsync(PinionCore.Memorys.Buffer buffer, CancellationToken token)
        {
            var sendCount = 0;
            do
            {
                var count = await _Stream.Send(buffer.Bytes.Array, buffer.Bytes.Offset + sendCount, buffer.Count - sendCount, token);
                if (count == 0 || token.IsCancellationRequested)
                    return 0;
                sendCount += count;

            } while (sendCount < buffer.Count);
            return sendCount;
        }

        
    }
}
