using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PinionCore.Memorys;
using PinionCore.Network;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway.Tests
{
    public class ListenableSubscriber : IDisposable
    {
        private readonly ConcurrentDictionary<IStreamable, StreamableHandler> _streamableHandlers;
        private readonly IPool _pool;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private bool _disposed;

        public event Action<IStreamable, IEnumerable<Memorys.Buffer>, PackageSender> MessageEvent;

        public ListenableSubscriber(IListenable listenable, IPool pool)
        {
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _streamableHandlers = new ConcurrentDictionary<IStreamable, StreamableHandler>();
            _cancellationTokenSource = new CancellationTokenSource();

            if (listenable != null)
            {
                listenable.StreamableEnterEvent += OnStreamableEnter;
                listenable.StreamableLeaveEvent += OnStreamableLeave;
            }
        }

        private void OnStreamableEnter(IStreamable streamable)
        {
            if (_disposed) return;

            System.Console.WriteLine("ListenableSubscriber.OnStreamableEnter: Adding new streamable handler");
            var handler = new StreamableHandler(streamable, _pool, MessageEvent, _cancellationTokenSource.Token);
            _streamableHandlers.TryAdd(streamable, handler);
            handler.Start();
            System.Console.WriteLine("ListenableSubscriber.OnStreamableEnter: Handler started");
        }

        private void OnStreamableLeave(IStreamable streamable)
        {
            if (_streamableHandlers.TryRemove(streamable, out var handler))
            {
                handler.Dispose();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _cancellationTokenSource?.Cancel();

            foreach (var handler in _streamableHandlers.Values)
            {
                handler.Dispose();
            }
            _streamableHandlers.Clear();

            _cancellationTokenSource?.Dispose();
        }

        private class StreamableHandler : IDisposable
        {
            private readonly IStreamable _streamable;
            private readonly PackageReader _reader;
            private readonly PackageSender _sender;
            private readonly Action<IStreamable, IEnumerable<Memorys.Buffer>, PackageSender> _messageEvent;
            private readonly CancellationToken _cancellationToken;
            private bool _disposed;

            public StreamableHandler(IStreamable streamable, IPool pool,
                Action<IStreamable, IEnumerable<Memorys.Buffer>, PackageSender> messageEvent,
                CancellationToken cancellationToken)
            {
                _streamable = streamable;
                _reader = new PackageReader(streamable, pool);
                _sender = new PackageSender(streamable, pool);
                _messageEvent = messageEvent;
                _cancellationToken = cancellationToken;
            }

            public void Start()
            {
                // Start the read loop immediately and ensure it's running
                var readTask = Task.Run(async () => await ReadLoopAsync(), _cancellationToken);
                // Give a small delay to ensure the read loop has started
                Task.Delay(10, _cancellationToken).Wait();
            }

            private async Task ReadLoopAsync()
            {
                try
                {
                    System.Console.WriteLine("StreamableHandler.ReadLoopAsync: Starting read loop");
                    while (!_cancellationToken.IsCancellationRequested && !_disposed)
                    {
                        System.Console.WriteLine("StreamableHandler.ReadLoopAsync: Waiting for data...");
                        var buffers = await _reader.Read();
                        if (_disposed || _cancellationToken.IsCancellationRequested)
                            break;

                        System.Console.WriteLine($"StreamableHandler.ReadLoopAsync: Received {buffers.Count} buffers");
                        _messageEvent?.Invoke(_streamable, buffers, _sender);
                    }
                }
                catch (OperationCanceledException)
                {
                    System.Console.WriteLine("StreamableHandler.ReadLoopAsync: Cancelled");
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"StreamableHandler.ReadLoopAsync: Exception {ex.GetType().Name}: {ex.Message}");
                }
            }

            public void Dispose()
            {
                _disposed = true;

                IDisposable readerDispose = _reader;
                readerDispose.Dispose();

                IDisposable senderDispose = _sender;
                senderDispose.Dispose();
            }
        }
    }
}
