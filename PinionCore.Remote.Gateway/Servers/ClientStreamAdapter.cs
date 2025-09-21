using System;
using System.Threading;
using System.Threading.Tasks;
using PinionCore.Network;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Servers
{
    public sealed class UserStreamAdapter : IStreamable, IDisposable
    {
        private readonly IServiceSession _user;
        private readonly Stream _stream;
        private readonly IStreamable _streamView;
        private readonly UserStreamRegistry.Bridge _bridge;
        private readonly CancellationTokenSource _pumpCancellation;
        private readonly Task _pumpTask;
        private bool _disposed;

        public UserStreamAdapter(IServiceSession user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            _user = user;
            _stream = new Stream();
            _streamView = _stream;
            var userId = user.Id.Value;
            if (!UserStreamRegistry.TryGet(userId, out _bridge))
            {
                throw new InvalidOperationException(string.Format("Cannot locate user stream bridge for id {0}.", userId));
            }

            _pumpCancellation = new CancellationTokenSource();
            _pumpTask = Task.Run(() => PumpAsync(_pumpCancellation.Token));
        }

        public IAwaitableSource<int> Receive(byte[] buffer, int offset, int count)
        {
            return _streamView.Receive(buffer, offset, count);
        }

        public IAwaitableSource<int> Send(byte[] buffer, int offset, int count)
        {
            if (count <= 0)
            {
                return 0.ToWaitableValue();
            }

            var payload = new byte[count];
            Array.Copy(buffer, offset, payload, 0, count);
            _user.Request(payload);
            return count.ToWaitableValue();
        }

        private async Task PumpAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var payload = await _bridge.DequeueAsync(token).ConfigureAwait(false);
                    if (payload == null)
                    {
                        continue;
                    }

                    _stream.Push(payload, 0, payload.Length);
                }
            }
            catch (TaskCanceledException)
            {
            }
            catch (OperationCanceledException)
            {
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _pumpCancellation.Cancel();
            try
            {
                _pumpTask.Wait();
            }
            catch (AggregateException ex)
            {
                if (!(ex.InnerException is TaskCanceledException) && !(ex.InnerException is OperationCanceledException))
                {
                    throw;
                }
            }
            finally
            {
                _pumpCancellation.Dispose();
            }
        }
    }
}
