using System;
using System.Collections.Generic;
using PinionCore.Network;

namespace PinionCore.Remote.Tests
{
    internal class SocketHeadReaderTestPeer : Network.IStreamable
    {
        private readonly Queue<byte> _Buffer;

        public SocketHeadReaderTestPeer()
        {
            _Buffer = new System.Collections.Generic.Queue<byte>(new byte[] { 0x85, 0x05, 0x95 });
        }





        IAwaitableSource<int> IStreamable.Receive(byte[] buffer, int offset, int count)
        {
            return System.Threading.Tasks.Task.Run(() =>
            {
                if (_Buffer.Count > 0)
                {
                    buffer[offset] = _Buffer.Dequeue();
                    buffer[offset + 1] = _Buffer.Dequeue();
                    buffer[offset + 2] = _Buffer.Dequeue();
                    return 3;
                }

                return 0;

            }).ToWaitableValue();


        }

        IAwaitableSource<int> IStreamable.Send(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
