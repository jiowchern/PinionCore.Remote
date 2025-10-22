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
        private readonly AsyncLocal<bool> _isInsideHandler = new AsyncLocal<bool>();
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

            executionOptions.TaskScheduler = effectiveOptions.TaskScheduler ?? TaskScheduler.Default;

            _actionBlock = new ActionBlock<TMessage>(
                message => ExecuteHandlerAsync(handler, message),
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

            var completion = _actionBlock.Completion;

            if (_isInsideHandler.Value)
            {
                ObserveCompletionAsync(completion);
                return;
            }

            if (_disposeTimeout != Timeout.InfiniteTimeSpan && _disposeTimeout >= TimeSpan.Zero)
            {
                try
                {
                    if (completion.Wait(_disposeTimeout))
                    {
                        FinalizeCompletion(completion);
                        return;
                    }
                }
                catch (OperationCanceledException)
                {
                    FinalizeCompletion(completion);
                    return;
                }
                catch (AggregateException)
                {
                    if (completion.IsFaulted)
                    {
                        _ = completion.Exception;
                    }

                    FinalizeCompletion(completion);
                    return;
                }
            }

            ObserveCompletionAsync(completion);
        }

        private async Task ExecuteHandlerAsync(Func<TMessage, CancellationToken, Task> handler, TMessage message)
        {
            _isInsideHandler.Value = true;

            try
            {
                await handler(message, _internalCancellation.Token).ConfigureAwait(false);
            }
            finally
            {
                _isInsideHandler.Value = false;
            }
        }

        private void ObserveCompletionAsync(Task completion)
        {
            if (completion.IsCompleted)
            {
                FinalizeCompletion(completion);
                return;
            }

            completion.ContinueWith(
                FinalizeCompletion,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        private void FinalizeCompletion(Task completion)
        {
            try
            {
                if (completion.IsFaulted)
                {
                    _ = completion.Exception;
                }
            }
            finally
            {
                _internalCancellation.Dispose();
            }
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
