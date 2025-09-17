using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PinionCore.Memorys;
using PinionCore.Network;
using Buffer = PinionCore.Memorys.Buffer;

namespace PinionCore.Remote.Gateway.Sessions
{
    class GatewaySessionConnector : IDisposable
    {
        private static int _GlobalSessionId;

        private readonly PackageReader _reader;
        private readonly PackageSender _sender;
        private readonly Serializer _serializer;
        private readonly Dictionary<IStreamable, Session> _streams;
        private readonly Dictionary<uint, Session> _sessionsById;
        private readonly CancellationTokenSource _cancellationSource;
        private readonly SemaphoreSlim _mutex;
        private Task _readTask;
        private bool _started;
        private bool _disposed;

        private sealed class Session
        {
            public Session(uint id, IStreamable stream, CancellationTokenSource cancellation)
            {
                Id = id;
                Stream = stream;
                Cancellation = cancellation;
                Buffer = new byte[4096];
            }

            public uint Id { get; }
            public IStreamable Stream { get; }
            public byte[] Buffer { get; }
            public CancellationTokenSource Cancellation { get; }
            public Task PumpTask { get; set; }
        }

        public GatewaySessionConnector(PackageReader reader, PackageSender sender, Serializer serializer)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

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
                var session = new Session(id, stream, sessionCancellation);

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

                session.PumpTask = PumpSessionAsync(session, sessionCancellation.Token);

                return id;
            }
            finally
            {
                _mutex.Release();
            }
        }

        private async Task LeaveAsync(IStreamable stream, CancellationToken token)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            await _mutex.WaitAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();

                if (!_streams.TryGetValue(stream, out var session))
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
        }

        private void _ReadDone(Task<List<Buffer>> task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            if (task.IsFaulted)
            {
                var exception = task.Exception ?? new AggregateException(new InvalidOperationException("Reader faulted."));
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
                    var payload = package.Payload ?? Array.Empty<byte>();
                    _ = session.Stream.Send(payload, 0, payload.Length);
                }
            }

            _readTask = _reader.Read().ContinueWith(_ReadDone);

        }


        private async Task HandleClientPackageAsync(ClientToServerPackage package, CancellationToken token)
        {
            await _mutex.WaitAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();

                var buffer = _serializer.Serialize(package);
                _sender.Push(buffer);
            }
            finally
            {
                _mutex.Release();
            }
        }

        private async Task PumpSessionAsync(Session session, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var count = await session.Stream.Receive(session.Buffer, 0, session.Buffer.Length);
                    if (count <= 0)
                    {
                        continue;
                    }

                    token.ThrowIfCancellationRequested();

                    var payload = new byte[count];
                    Array.Copy(session.Buffer, 0, payload, 0, count);

                    var package = new ClientToServerPackage
                    {
                        OpCode = OpCodeClientToServer.Message,
                        Id = session.Id,
                        Payload = payload
                    };

                    await HandleClientPackageAsync(package, token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception)
            {
                _cancellationSource.Cancel();
            }
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
                try
                {
                    session.PumpTask?.Wait(TimeSpan.FromSeconds(1));
                }
                catch (AggregateException ex)
                {
                    ex.Handle(e => e is OperationCanceledException);
                }

                session.Cancellation.Dispose();
            }

            _cancellationSource.Dispose();
            _mutex.Dispose();
            _streams.Clear();
            _sessionsById.Clear();
        }
    }
}
