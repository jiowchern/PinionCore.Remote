using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using PinionCore.Memorys;
using PinionCore.Network;

namespace PinionCore.Remote.Gateway
{
    internal sealed class UserSession : IDisposable
    {
        private readonly Channel _channel;
        private readonly IPool _pool;
        private bool _disposed;

        public UserSession(uint id, IStreamable stream, IPool pool)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            Id = id;
            Stream = stream;
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _channel = new Channel(new PackageReader(stream, pool), new PackageSender(stream, pool));
            _channel.OnDataReceived += _HandleIncoming;
            _channel.OnDisconnected += _HandleDisconnected;
        }

        public uint Id { get; }

        public IStreamable Stream { get; }

        public event Action<UserSession, List<Memorys.Buffer>> MessageReceived;

        public event Action<UserSession> Disconnected;

        internal void Start()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(UserSession));
            }

            _channel.Start();
        }

        internal void SendToUser(uint group, byte[] payload)
        {
            if (_disposed)
            {
                return;
            }

            var data = payload ?? Array.Empty<byte>();
            var buffer = _pool.Alloc(data.Length + sizeof(uint));
            var segment = buffer.Bytes;
            BinaryPrimitives.WriteUInt32LittleEndian(segment.Array.AsSpan(segment.Offset, sizeof(uint)), group);
            if (data.Length > 0)
            {
                Array.Copy(data, 0, segment.Array, segment.Offset + sizeof(uint), data.Length);
            }

            _channel.Sender.Push(buffer);
        }

        private List<Memorys.Buffer> _HandleIncoming(List<Memorys.Buffer> buffers)
        {
            MessageReceived?.Invoke(this, buffers);
            return new List<Memorys.Buffer>();
        }

        private void _HandleDisconnected()
        {
            Disconnected?.Invoke(this);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _channel.OnDataReceived -= _HandleIncoming;
            _channel.OnDisconnected -= _HandleDisconnected;
            _channel.Dispose();
        }
    }
}
