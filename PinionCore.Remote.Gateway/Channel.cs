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
        public readonly PackageSender Sender;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public event System.Func<List<Memorys.Buffer>, List<Memorys.Buffer>> OnDataReceived;
        public event System.Action OnDisconnected;
        bool _disposed;

        public Channel(PackageReader reader, PackageSender sender)
        {
            _reader = reader;
            Sender = sender;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void Start()
        {
            Task.Run(async () => await _ReadLoopAsync(), _cancellationTokenSource.Token);
        }

        private async Task _ReadLoopAsync()
        {
            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested && !_disposed)
                {
                    var buffers = await _reader.Read();
                    if (_disposed || _cancellationTokenSource.Token.IsCancellationRequested)
                        break;

                    if (buffers.Count == 0)
                    {
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
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
            catch (Exception)
            {
                OnDisconnected?.Invoke();
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }
    }
}
