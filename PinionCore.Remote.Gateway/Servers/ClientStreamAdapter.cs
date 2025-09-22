using System;
using System.Threading;
using System.Threading.Tasks;
using PinionCore.Network;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Servers
{
    public sealed class ClientStreamAdapter : IStreamable, IDisposable
    {
        private readonly IClientConnection _client;
        private readonly Stream _stream;
        private readonly IStreamable _streamView;
        private readonly ClientStreamRegistry.Bridge _bridge;
        private readonly CancellationTokenSource _pumpCancellation;
        private readonly Task _pumpTask;
        private bool _disposed;

        public ClientStreamAdapter(IClientConnection client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            _client = client;
            _stream = new Stream();
            _streamView = _stream;
            var clientId = client.Id.Value;
            if (!ClientStreamRegistry.TryGet(clientId, out _bridge))
            {
                throw new InvalidOperationException(string.Format("Cannot locate client stream bridge for id {0}.", clientId));
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
            _client.Request(payload);
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
