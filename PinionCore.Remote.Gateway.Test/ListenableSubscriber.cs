using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using PinionCore.Memorys;
using PinionCore.Network;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway.Tests
{
    public class ListenableSubscriber : IDisposable
    {
        private readonly List<IDisposable> _disposables;
        private readonly IPool _pool;

        public event Action<IStreamable, IEnumerable<Memorys.Buffer>, PackageSender> MessageEvent;

        public ListenableSubscriber(IListenable listenable, IPool pool)
        {
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _disposables = new List<IDisposable>();

            if (listenable != null)
            {
                Subscribe(listenable);
            }
        }

        private void Subscribe(IListenable listenable)
        {
            var streamableObservable = Observable.FromEvent<Action<IStreamable>, IStreamable>(
                h => listenable.StreamableEnterEvent += h,
                h => listenable.StreamableEnterEvent -= h);

            var subscription = streamableObservable.Subscribe(streamable =>
            {
                Subscribe(streamable);
            });

            _disposables.Add(subscription);
        }

        private void Subscribe(IStreamable streamable)
        {
            var reader = new PackageReader(streamable, _pool);
            var sender = new PackageSender(streamable, _pool);

            var bufferObservable = Observable.Create<Memorys.Buffer>(observer =>
            {
                var cts = new CancellationTokenSource();
                var pumpTask = Task.Run(async () =>
                {
                    try
                    {
                        while (!cts.Token.IsCancellationRequested)
                        {
                            var buffers = await reader.Read(cts.Token).ConfigureAwait(false);
                            if (buffers == null || buffers.Count == 0)
                            {
                                continue;
                            }

                            foreach (var buffer in buffers)
                            {
                                observer.OnNext(buffer);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        observer.OnError(ex);
                        return;
                    }

                    observer.OnCompleted();
                });

                return Disposable.Create(() =>
                {
                    cts.Cancel();
                    try
                    {
                        pumpTask.Wait(TimeSpan.FromSeconds(1));
                    }
                    catch (AggregateException ex)
                    {
                        ex.Handle(e => e is OperationCanceledException);
                    }
                    finally
                    {
                        cts.Dispose();
                    }
                });
            });

            var subscription = bufferObservable.Subscribe(buffer =>
            {
                var buffers = new[] { buffer };
                MessageEvent?.Invoke(streamable, buffers, sender);
            });

            _disposables.Add(subscription);
        }

        public void Dispose()
        {
            foreach (var disposable in _disposables)
            {
                disposable?.Dispose();
            }
            _disposables.Clear();
        }
    }
}