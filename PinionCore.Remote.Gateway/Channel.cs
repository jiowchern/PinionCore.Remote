using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PinionCore.Network;

namespace PinionCore.Remote.Gateway
{
    class Channel : IDisposable
    {
        
        readonly PackageReader _reader;
        Task _readTask;
        public readonly PackageSender Sender;

        public event System.Func<List<Memorys.Buffer>, List<Memorys.Buffer>> OnDataReceived;
        public event System.Action OnDisconnected;
        bool _disposed;       

        public Channel(PackageReader reader, PackageSender sender)
        {
            _reader = reader;
            Sender = sender;
            _readTask = Task.CompletedTask;
        }

        public void Start()
        {
            _StartRead();
        }

        private void _StartRead()
        {
            _readTask = _reader.Read().ContinueWith(_ReadDone);
        }


        private void _ReadDone(Task<List<Memorys.Buffer>> task)
        {
            if (_disposed)
                return;
            var buffers = task.Result;
            if(buffers.Count == 0)
            {
                OnDisconnected.Invoke();
                return;
            }
            var sends = OnDataReceived.Invoke(buffers);
            foreach(var send in sends)
            {
                Sender.Push(send);
            }

            _StartRead();
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
