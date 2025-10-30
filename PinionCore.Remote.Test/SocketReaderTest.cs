using System;
using PinionCore.Network;

namespace PinionCore.Remote.Tests
{
    public class SocketReaderTestPeer : IStreamable
    {

        public byte[] Buffer = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        public System.Collections.Generic.Queue<int> ReadStepQueue = new System.Collections.Generic.Queue<int>(
            new int[] { 10 }
            );

        IAwaitableSource<int> IStreamable.Receive(byte[] buffer, int offset, int count)
        {
            return System.Threading.Tasks.Task<int>.Run(() =>
            {
                if (ReadStepQueue.Count == 0)
                    return 0;
                var step = ReadStepQueue.Dequeue();
                var count = 0;
                for (byte i = 0; i < step; ++i)
                {
                    buffer[offset + i] = Buffer[i];
                    count++;
                }
                return count;
            }).ToWaitableValue();

        }

        IAwaitableSource<int> IStreamable.Send(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        void IDisposable.Dispose()
        {
            // 測試類，不需要清理資源
        }
    }



}
