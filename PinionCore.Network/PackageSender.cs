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

        // 保護 _sendQueue 與 _isSending:Soul 端 response push 可能同時來自
        // 遊戲邏輯執行緒(property/event 同步)與封包迴圈執行緒(ping/method 回傳)
        private readonly object _Sync = new object();
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

        // detachPumpContext:啟動 pump 時暫時清掉 SynchronizationContext,讓整條送出鏈
        // 落在 thread pool 而不被啟動端執行緒(如 Unity 主執行緒)的 context 錨定;
        // 單執行緒環境(WebGL client)必須維持 false
        private readonly bool _DetachPumpContext;

        public PackageSender(IStreamable stream, PinionCore.Memorys.IPool pool, bool detachPumpContext = false)
        {
            _Stream = stream;
            _Pool = pool;
            _DetachPumpContext = detachPumpContext;
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

            lock (_Sync)
            {
                _sendQueue.Enqueue(new PendingSend(sendBuffer, cancellation));

                // 「標記 sending」與 enqueue 同鎖:兩條執行緒同時 Push 時只有一條啟動 pump
                if (_isSending)
                    return;
                _isSending = true;
            }

            if (_DetachPumpContext && SynchronizationContext.Current != null)
            {
                SynchronizationContext previous = SynchronizationContext.Current;
                SynchronizationContext.SetSynchronizationContext(null);
                try
                {
                    _ = _ProcessSendQueueAsync();
                }
                finally
                {
                    SynchronizationContext.SetSynchronizationContext(previous);
                }
            }
            else
            {
                _ = _ProcessSendQueueAsync();
            }
        }

        void IDisposable.Dispose()
        {
            // Unity WebGL 不能同步等待 Task，避免死鎖
            // 清理佇列中的 buffer
            lock (_Sync)
            {
                while (_sendQueue.Count > 0)
                {
                    var pending = _sendQueue.Dequeue();
                    var buffer = pending.Buffer;
                 //   buffer.Dispose();
                }
            }
        }

        private async Task _ProcessSendQueueAsync()
        {
            while (true)
            {
                PendingSend pending;
                lock (_Sync)
                {
                    // 「清旗標」與「判空」同鎖:Push 端不會在 pump 收尾瞬間漏掉新項目
                    if (_sendQueue.Count == 0)
                    {
                        _isSending = false;
                        return;
                    }
                    pending = _sendQueue.Dequeue();
                }

                if (pending.Token.IsCancellationRequested)
                    continue;

                try
                {
                    await _SendBufferAsync(pending.Buffer, pending.Token);
                }
                catch (Exception e)
                {
                    // socket 中途 dispose 等例外不能讓 fire-and-forget 的 pump 變成 unobserved exception
                    PinionCore.Utility.Log.Instance.WriteInfo($"PackageSender pump error: {e}");
                    lock (_Sync)
                    {
                        _isSending = false;
                    }
                    return;
                }
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
