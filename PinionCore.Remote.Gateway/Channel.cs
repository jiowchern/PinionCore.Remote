using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PinionCore.Network;

namespace PinionCore.Remote.Gateway
{
    class Channel : IDisposable
    {

        readonly PackageReader _reader;
        Task _readTask;
        public readonly PackageSender Sender;
        readonly CancellationTokenSource _cancellationSource;
        bool _cancellationDisposed;

        public event System.Func<List<Memorys.Buffer>, List<Memorys.Buffer>> OnDataReceived;
        public event System.Action OnDisconnected;
        bool _disposed;

        public Channel(PackageReader reader, PackageSender sender)
        {
            _reader = reader;
            Sender = sender;
            _readTask = Task.CompletedTask;
            _cancellationSource = new CancellationTokenSource();
        }

        public void Start()
        {
            _StartRead();
        }

        private void _StartRead()
        {
            if (_cancellationSource.IsCancellationRequested)
            {
                return;
            }

            _readTask = _reader
                .Read(_cancellationSource.Token)
                .ContinueWith(
                    _ReadDone,
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
        }


        private void _ReadDone(Task<List<Memorys.Buffer>> task)
        {
            if (_disposed)
                return;
            if (task.IsCanceled)
            {
                return;
            }

            if (task.IsFaulted)
            {
                var exception = task.Exception?.InnerException ?? task.Exception;
                if (exception is OperationCanceledException)
                {
                    return;
                }

                throw exception;
            }

            List<Memorys.Buffer> buffers;
            try
            {
                buffers = task.Result;
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (buffers.Count == 0)
            {
                _CancelAndDisposeCancellation();
                OnDisconnected?.Invoke();
                return;
            }
            var sends = OnDataReceived?.Invoke(buffers);
            if (sends != null)
            {
                foreach (var send in sends)
                {
                    Sender.Push(send);
                }
            }

            _StartRead();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _CancelAndDisposeCancellation();
            try
            {
                _readTask?.Wait(TimeSpan.FromSeconds(1));
            }
            catch (AggregateException ex)
            {
                ex.Handle(e => e is OperationCanceledException);
            }
        }

        void _CancelAndDisposeCancellation()
        {
            if (_cancellationDisposed)
            {
                return;
            }

            _cancellationSource.Cancel();
            _cancellationSource.Dispose();
            _cancellationDisposed = true;
        }
    }
}
