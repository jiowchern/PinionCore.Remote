﻿using Regulus.Network;
using System;
using System.Collections.Generic;

namespace Regulus.Remote.Tests
{
    internal class SocketHeadReaderTestPeer : Network.IStreamable
    {
        private readonly Queue<byte> _Buffer;

        public SocketHeadReaderTestPeer()
        {
            _Buffer = new System.Collections.Generic.Queue<byte>(new byte[] { 0x85, 0x05 });
        }





        IWaitableValue<int> IStreamable.Receive(byte[] buffer, int offset, int count)
        {
            return System.Threading.Tasks.Task.Run(() =>
            {
                buffer[offset] = _Buffer.Dequeue();
                return 1;
            }).ToWaitableValue();


        }

        IWaitableValue<int> IStreamable.Send(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}