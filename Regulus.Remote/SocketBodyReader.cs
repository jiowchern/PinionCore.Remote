﻿using Regulus.Network;
using Regulus.Utility;
using System;

namespace Regulus.Remote
{
    internal class SocketBodyReader : ISocketReader
    {
        public event OnByteDataCallback DoneEvent;

        public event OnErrorCallback ErrorEvent;

        private readonly IStreamable _Peer;

        private byte[] _Buffer;

        private int _Offset;


        public SocketBodyReader(IStreamable peer)
        {

            this._Peer = peer;
        }



        internal System.Threading.Tasks.Task Read(int size)
        {            
            _Offset = 0;
            _Buffer = new byte[size];
            try
            {
                System.Threading.Tasks.Task<int> task = _Peer.Receive(_Buffer, _Offset, _Buffer.Length - _Offset);                
                return task.ContinueWith(t => _Readed(t.Result));
            }
            catch (SystemException e)
            {
                if (ErrorEvent != null)
                {
                    ErrorEvent();
                }
            }

            return System.Threading.Tasks.Task.Factory.StartNew(() => { });
        }

        private void _Readed(int read_count)
        {            
            int readSize = read_count;

            _Offset += readSize;
            NetworkMonitor.Instance.Read.Set(readSize);
            if (_Offset == _Buffer.Length)
            {            
                
                DoneEvent(_Buffer);
            }
            else                 
            {
                System.Threading.Tasks.Task<int> task = _Peer.Receive(
                    _Buffer,
                    _Offset,
                    _Buffer.Length - _Offset);
                task.ContinueWith(t => _Readed(t.Result));
            }
        }

        

        void IBootable.Launch()
        {
            
        }

        void IBootable.Shutdown()
        {
        }

       
    }
}