using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using PinionCore.Network;

namespace PinionCore.Remote.Gateway.Servers
{
    internal static class GatewayServerClientChannelRegistry
    {
        internal sealed class Bridge : System.IDisposable
        {
            private readonly ConcurrentQueue<byte[]> _outgoing;
            private readonly SemaphoreSlim _signal;
            private volatile bool _disposed;

            internal IStreamable ServerStream { get; }

            internal Bridge(IStreamable serverStream)
            {
                ServerStream = serverStream;
                _outgoing = new ConcurrentQueue<byte[]>();
                _signal = new SemaphoreSlim(0);
            }

            internal void Enqueue(byte[] payload)
            {
                if (_disposed)
                {
                    return;
                }

                _outgoing.Enqueue(payload);
                _signal.Release();
            }

            internal async Task<byte[]> DequeueAsync(CancellationToken token)
            {
                while (!token.IsCancellationRequested)
                {
                    byte[] payload;
                    if (_outgoing.TryDequeue(out payload))
                    {
                        return payload;
                    }

                    await _signal.WaitAsync(token).ConfigureAwait(false);
                }

                token.ThrowIfCancellationRequested();
                return null;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                try
                {
                    _signal.Release();
                }
                catch (System.ObjectDisposedException)
                {
                }
                _signal.Dispose();
                while (_outgoing.TryDequeue(out _))
                {
                }
            }
        }

        private static readonly ConcurrentDictionary<uint, Bridge> _bridges = new ConcurrentDictionary<uint, Bridge>();

        internal static void Register(uint channelId, IStreamable serverStream)
        {
            var bridge = new Bridge(serverStream);
            _bridges[channelId] = bridge;
        }

        internal static void Unregister(uint channelId)
        {
            Bridge bridge;
            if (_bridges.TryRemove(channelId, out bridge))
            {
                bridge.Dispose();
            }
        }

        internal static bool TryGet(uint channelId, out Bridge bridge)
        {
            return _bridges.TryGetValue(channelId, out bridge);
        }

        internal static void Enqueue(uint channelId, byte[] payload)
        {
            Bridge bridge;
            if (_bridges.TryGetValue(channelId, out bridge))
            {
                bridge.Enqueue(payload);
            }
        }
    }
}


