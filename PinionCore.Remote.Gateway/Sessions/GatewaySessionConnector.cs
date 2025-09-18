using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PinionCore.Memorys;
using PinionCore.Network;
using static System.Collections.Specialized.BitVector32;
using Buffer = PinionCore.Memorys.Buffer;

namespace PinionCore.Remote.Gateway.Sessions
{
    class GatewaySessionConnector : IDisposable
    {
        private sealed class Session : IDisposable
        {
            private bool _disposed;
            private Task<List<Buffer>> _readTesk;

            public Session(uint id, IStreamable stream, CancellationTokenSource cancellation, IPool pool)
            {
                if (stream == null)
                {
                    throw new ArgumentNullException(nameof(stream));
                }

                if (cancellation == null)
                {
                    throw new ArgumentNullException(nameof(cancellation));
                }

                if (pool == null)
                {
                    throw new ArgumentNullException(nameof(pool));
                }

                Id = id;
                Stream = stream;
                Cancellation = cancellation;
                Reader = new PackageReader(stream, pool);
                Sender = new PackageSender(stream, pool);
                ReadTask = Task.CompletedTask;
            }

            public uint Id { get; }
            public IStreamable Stream { get; }
            public PackageReader Reader { get; }
            public Task ReadTask;
            public PackageSender Sender { get; }
            public CancellationTokenSource Cancellation { get; }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                Cancellation.Dispose();
            }


        }
        private static int _GlobalSessionId;

        private readonly PackageReader _reader;
        private readonly PackageSender _sender;
        private readonly Serializer _serializer;
        private readonly IPool _pool;
        private readonly Dictionary<IStreamable, Session> _streams;
        private readonly Dictionary<uint, Session> _sessionsById;
        private readonly CancellationTokenSource _cancellationSource;
        private readonly SemaphoreSlim _mutex;
        private Task _readTask;
        private bool _started;
        private bool _disposed;

        

        public GatewaySessionConnector(PackageReader reader, PackageSender sender, Serializer serializer)
            : this(reader, sender, serializer, PinionCore.Memorys.PoolProvider.Shared)
        {
        }

        public GatewaySessionConnector(PackageReader reader, PackageSender sender, Serializer serializer, IPool pool)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));

            _streams = new Dictionary<IStreamable, Session>();
            _sessionsById = new Dictionary<uint, Session>();
            _cancellationSource = new CancellationTokenSource();
            _mutex = new SemaphoreSlim(1, 1);

            _readTask = Task.CompletedTask;
        }

        public void Start()
        {
            ThrowIfDisposed();

            if (_started)
            {
                return;
            }

            _started = true;
            _readTask = _reader.Read().ContinueWith(_ReadDone);
        }

        public uint Join(IStreamable stream)
        {
            ThrowIfDisposed();

            return JoinAsync(stream, _cancellationSource.Token).GetAwaiter().GetResult();
        }

        public void Leave(IStreamable stream)
        {
            ThrowIfDisposed();

            LeaveAsync(stream, _cancellationSource.Token).GetAwaiter().GetResult();
        }

        private async Task<uint> JoinAsync(IStreamable stream, CancellationToken token)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            await _mutex.WaitAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();

                if (_streams.TryGetValue(stream, out var existing))
                {
                    return existing.Id;
                }

                var id = (uint)Interlocked.Increment(ref _GlobalSessionId);
                var sessionCancellation = CancellationTokenSource.CreateLinkedTokenSource(_cancellationSource.Token);
                var session = new Session(id, stream, sessionCancellation, _pool);

                _streams.Add(stream, session);
                _sessionsById.Add(id, session);

                var joinPackage = new ClientToServerPackage
                {
                    OpCode = OpCodeClientToServer.Join,
                    Id = id,
                    Payload = Array.Empty<byte>()
                };

                var buffer = _serializer.Serialize(joinPackage);
                _sender.Push(buffer);



                _StartReading(session);

                return id;
            }
            finally
            {
                _mutex.Release();
            }
        }

        private void _StartReading(Session session)
        {
            session.ReadTask = session.Reader.Read().ContinueWith((t)=> { _ReadSessionDone(t, session); });
        }

        private void _ReadSessionDone(Task<List<Buffer>> task, Session session)
        {
            var buffers = task.Result;
            foreach (var buffer in buffers)
            {
                

                var segment = buffer.Bytes;
                var count = segment.Count;
                if (count <= 0 || segment.Array == null)
                {
                    continue;
                }

                var headerLength = PinionCore.Serialization.Varint.GetByteCount(count);
                var payload = new byte[headerLength + count];
                var bodyOffset = PinionCore.Serialization.Varint.NumberToBuffer(payload, 0, count);
                Array.Copy(segment.Array, segment.Offset, payload, bodyOffset, count);

                var package = new ClientToServerPackage
                {
                    OpCode = OpCodeClientToServer.Message,
                    Id = session.Id,
                    Payload = payload
                };

                
                _sender.Push(_serializer.Serialize(package));
            }

            _StartReading(session);
        }

        private async Task LeaveAsync(IStreamable stream, CancellationToken token)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            Session session = null;

            await _mutex.WaitAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();

                if (!_streams.TryGetValue(stream, out session))
                {
                    return;
                }

                _streams.Remove(stream);
                _sessionsById.Remove(session.Id);

                session.Cancellation.Cancel();

                var leavePackage = new ClientToServerPackage
                {
                    OpCode = OpCodeClientToServer.Leave,
                    Id = session.Id,
                    Payload = Array.Empty<byte>()
                };

                var buffer = _serializer.Serialize(leavePackage);
                _sender.Push(buffer);
            }
            finally
            {
                _mutex.Release();
            }

            if (session != null)
            {
                await CleanupSessionAsync(session).ConfigureAwait(false);
            }
        }

        private void _ReadDone(Task<List<Buffer>> task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            if (task.IsFaulted)
            {
                return;
            }

            if (task.IsCanceled || _disposed)
            {
                return;
            }

            var buffers = task.Result;
            if (buffers == null || buffers.Count == 0)
            {
                return;
            }

            foreach (var buffer in buffers)
            {
                var package = (ServerToClientPackage)_serializer.Deserialize(buffer);
                if (package.OpCode == OpCodeServerToClient.Message &&
                    _sessionsById.TryGetValue(package.Id, out var session))
                {
                    var payload = package.Payload;
                    if (payload == null || payload.Length == 0)
                    {
                        continue;
                    }

                    var headerLength = PinionCore.Serialization.Varint.BufferToNumber(payload, 0, out int bodyLength);
                    if (headerLength <= 0 || bodyLength <= 0 || payload.Length < headerLength + bodyLength)
                    {
                        continue;
                    }

                    var outbound = _pool.Alloc(bodyLength);
                    Array.Copy(payload, headerLength, outbound.Bytes.Array, outbound.Bytes.Offset, bodyLength);
                    session.Sender.Push(outbound);
                }
            }

            _readTask = _reader.Read().ContinueWith(_ReadDone);
        }

        

        

        private async Task CleanupSessionAsync(Session session)
        {
            if (session == null)
            {
                return;
            }

            

            session.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(GatewaySessionConnector));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _cancellationSource.Cancel();

            try
            {
                _readTask?.Wait(TimeSpan.FromSeconds(1));
            }
            catch (AggregateException ex)
            {
                ex.Handle(e => e is OperationCanceledException);
            }

            foreach (var session in _streams.Values)
            {
                session.Cancellation.Cancel();
            }

            foreach (var session in _streams.Values)
            {
                CleanupSessionAsync(session).GetAwaiter().GetResult();
            }

            _cancellationSource.Dispose();
            _mutex.Dispose();
            _streams.Clear();
            _sessionsById.Clear();
        }
    }
}
