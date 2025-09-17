using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace PinionCore.Remote.Actors
{
    public sealed class DataflowActor<TMessage> : IDisposable
    {
        private readonly ActionBlock<TMessage> _actionBlock;
        private readonly CancellationTokenSource _internalCancellation;
        private readonly TimeSpan _disposeTimeout;
        private bool _disposed;

        public DataflowActor(Func<TMessage, CancellationToken, Task> handler)
            : this(handler, ActorOptions.Default)
        {
        }

        public DataflowActor(Func<TMessage, CancellationToken, Task> handler, ActorOptions options)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var effectiveOptions = options == null ? ActorOptions.Default : options.Clone();

            _disposeTimeout = effectiveOptions.DisposeTimeout;
            _internalCancellation = CancellationTokenSource.CreateLinkedTokenSource(effectiveOptions.CancellationToken);

            var executionOptions = new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = effectiveOptions.BoundedCapacity,
                CancellationToken = _internalCancellation.Token,
                EnsureOrdered = true,
                MaxDegreeOfParallelism = 1
            };

            if (effectiveOptions.TaskScheduler != null)
            {
                executionOptions.TaskScheduler = effectiveOptions.TaskScheduler;
            }

            _actionBlock = new ActionBlock<TMessage>(
                message => handler(message, _internalCancellation.Token),
                executionOptions);
        }

        public DataflowActor(Action<TMessage> handler)
            : this(handler, ActorOptions.Default)
        {
        }

        public DataflowActor(Action<TMessage> handler, ActorOptions options)
            : this(CreateHandler(handler), options)
        {
        }

        public Task Completion
        {
            get { return _actionBlock.Completion; }
        }

        public bool Post(TMessage message)
        {
            return _actionBlock.Post(message);
        }

        public Task<bool> SendAsync(TMessage message, CancellationToken cancellationToken = default)
        {
            return DataflowBlock.SendAsync(_actionBlock, message, cancellationToken);
        }

        public void Complete()
        {
            _actionBlock.Complete();
        }

        public void Cancel()
        {
            _internalCancellation.Cancel();
            _actionBlock.Complete();
        }

        public void Fault(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            ((IDataflowBlock)_actionBlock).Fault(exception);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _internalCancellation.Cancel();
            _actionBlock.Complete();

            try
            {
                if (_disposeTimeout == Timeout.InfiniteTimeSpan)
                {
                    _actionBlock.Completion.GetAwaiter().GetResult();
                }
                else if (_disposeTimeout >= TimeSpan.Zero)
                {
                    _actionBlock.Completion.Wait(_disposeTimeout);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception)
            {
                if (_actionBlock.Completion.IsFaulted)
                {
                    _ = _actionBlock.Completion.Exception;
                }
            }

            _internalCancellation.Dispose();
        }

        private static Func<TMessage, CancellationToken, Task> CreateHandler(Action<TMessage> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            return (message, token) =>
            {
                handler(message);
                return Task.CompletedTask;
            };
        }
    }
}
