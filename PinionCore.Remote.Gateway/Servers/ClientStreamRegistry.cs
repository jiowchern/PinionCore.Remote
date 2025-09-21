using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using PinionCore.Network;

namespace PinionCore.Remote.Gateway.Servers
{
    internal static class UserStreamRegistry
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

        private static readonly ConcurrentDictionary<uint, Bridge> Bridges = new ConcurrentDictionary<uint, Bridge>();

        internal static void Register(uint userId, IStreamable serverStream)
        {
            var bridge = new Bridge(serverStream);
            Bridges[userId] = bridge;
        }

        internal static void Unregister(uint userId)
        {
            Bridge bridge;
            if (Bridges.TryRemove(userId, out bridge))
            {
                bridge.Dispose();
            }
        }

        internal static bool TryGet(uint userId, out Bridge bridge)
        {
            return Bridges.TryGetValue(userId, out bridge);
        }

        internal static void Enqueue(uint userId, byte[] payload)
        {
            Bridge bridge;
            if (Bridges.TryGetValue(userId, out bridge))
            {
                bridge.Enqueue(payload);
            }
        }
    }
}
