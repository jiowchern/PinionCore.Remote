using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PinionCore.Memorys;
using PinionCore.Network;
using PinionCore.Remote.Actors;
using Buffer = PinionCore.Memorys.Buffer;

namespace PinionCore.Remote.Gateway.Sessions
{
    class GatewaySessionConnector : IDisposable
    {
        interface ActorCommand
        {
        }

        struct JoinCommand : ActorCommand
        {
            public IStreamable Stream;
            public TaskCompletionSource<uint> CompletionSource;
        }

        struct LeaveCommand : ActorCommand
        {
            public IStreamable Stream;
            public TaskCompletionSource<bool> CompletionSource;
        }

        struct IncomingDataCommand : ActorCommand
        {
            public uint SessionId;
            public List<Buffer> Buffers;
        }

        struct OutgoingDataCommand : ActorCommand
        {
            public uint SessionId;
            public byte[] Payload;
        }

        private sealed class Session : IDisposable
        {
            private bool _disposed;

            public Session(uint id, IStreamable stream, Channel channel, IPool pool)
            {
                if (stream == null)
                {
                    throw new ArgumentNullException(nameof(stream));
                }

                if (channel == null)
                {
                    throw new ArgumentNullException(nameof(channel));
                }

                if (pool == null)
                {
                    throw new ArgumentNullException(nameof(pool));
                }

                Id = id;
                Stream = stream;
                Channel = channel;
                Pool = pool;
            }

            public uint Id { get; }
            public IStreamable Stream { get; }
            public Channel Channel { get; }
            public IPool Pool { get; }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                Channel.Dispose();
            }
        }
        private static int _GlobalSessionId;

        readonly Channel _Channel;
        readonly DataflowActor<ActorCommand> _DataflowActor;
        private readonly Serializer _serializer;
        private readonly IPool _pool;
        private readonly Dictionary<IStreamable, Session> _streams;
        private readonly Dictionary<uint, Session> _sessionsById;
        private bool _started;
        private bool _disposed;

        public GatewaySessionConnector(PackageReader reader, PackageSender sender, Serializer serializer)
            : this(reader, sender, serializer, PinionCore.Memorys.PoolProvider.Shared)
        {
        }

        public GatewaySessionConnector(PackageReader reader, PackageSender sender, Serializer serializer, IPool pool)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));

            _DataflowActor = new DataflowActor<ActorCommand>(_Handle);
            _Channel = new Channel(reader, sender);

            _streams = new Dictionary<IStreamable, Session>();
            _sessionsById = new Dictionary<uint, Session>();
        }

        private void _Handle(ActorCommand command)
        {
            switch (command)
            {
                case JoinCommand join:
                    _HandleJoin(join);
                    break;
                case LeaveCommand leave:
                    _HandleLeave(leave);
                    break;
                case IncomingDataCommand incoming:
                    _HandleIncomingData(incoming);
                    break;
                case OutgoingDataCommand outgoing:
                    _HandleOutgoingData(outgoing);
                    break;
            }
        }

        public void Start()
        {
            ThrowIfDisposed();

            if (_started)
            {
                return;
            }

            _Channel.OnDataReceived += _ReadDone;
            _Channel.OnDisconnected += Dispose;
            _Channel.Start();

            _started = true;
        }

        public uint Join(IStreamable stream)
        {
            ThrowIfDisposed();

            var tcs = new TaskCompletionSource<uint>();
            var command = new JoinCommand { Stream = stream, CompletionSource = tcs };

            if (!_DataflowActor.Post(command))
            {
                throw new InvalidOperationException("Failed to post join command");
            }

            return tcs.Task.GetAwaiter().GetResult();
        }

        public void Leave(IStreamable stream)
        {
            ThrowIfDisposed();

            var tcs = new TaskCompletionSource<bool>();
            var command = new LeaveCommand { Stream = stream, CompletionSource = tcs };

            if (!_DataflowActor.Post(command))
            {
                throw new InvalidOperationException("Failed to post leave command");
            }

            tcs.Task.GetAwaiter().GetResult();
        }

        private void _HandleJoin(JoinCommand command)
        {
            try
            {
                if (command.Stream == null)
                {
                    command.CompletionSource.SetException(new ArgumentNullException(nameof(command.Stream)));
                    return;
                }

                if (_streams.TryGetValue(command.Stream, out var existing))
                {
                    command.CompletionSource.SetResult(existing.Id);
                    return;
                }

                var id = (uint)Interlocked.Increment(ref _GlobalSessionId);
                var sessionChannel = new Channel(new PackageReader(command.Stream, _pool), new PackageSender(command.Stream, _pool));
                var session = new Session(id, command.Stream, sessionChannel, _pool);

                _streams.Add(command.Stream, session);
                _sessionsById.Add(id, session);

                var joinPackage = new ClientToServerPackage
                {
                    OpCode = OpCodeClientToServer.Join,
                    Id = id,
                    Payload = Array.Empty<byte>()
                };

                var buffer = _serializer.Serialize(joinPackage);
                _Channel.Sender.Push(buffer);

                sessionChannel.OnDataReceived += (buffers) => {
                    _DataflowActor.Post(new IncomingDataCommand { SessionId = id, Buffers = buffers });
                    return new List<Buffer>();
                };
                sessionChannel.OnDisconnected += () => {
                    _DataflowActor.Post(new LeaveCommand { Stream = command.Stream, CompletionSource = new TaskCompletionSource<bool>() });
                };
                sessionChannel.Start();

                command.CompletionSource.SetResult(id);
            }
            catch (Exception ex)
            {
                command.CompletionSource.SetException(ex);
            }
        }

        private void _HandleLeave(LeaveCommand command)
        {
            try
            {
                if (command.Stream == null)
                {
                    command.CompletionSource.SetException(new ArgumentNullException(nameof(command.Stream)));
                    return;
                }

                if (!_streams.TryGetValue(command.Stream, out var session))
                {
                    command.CompletionSource.SetResult(true);
                    return;
                }

                _streams.Remove(command.Stream);
                _sessionsById.Remove(session.Id);

                var leavePackage = new ClientToServerPackage
                {
                    OpCode = OpCodeClientToServer.Leave,
                    Id = session.Id,
                    Payload = Array.Empty<byte>()
                };

                var buffer = _serializer.Serialize(leavePackage);
                _Channel.Sender.Push(buffer);

                session.Dispose();
                command.CompletionSource.SetResult(true);
            }
            catch (Exception ex)
            {
                command.CompletionSource.SetException(ex);
            }
        }

        private void _HandleIncomingData(IncomingDataCommand command)
        {
            foreach (var buffer in command.Buffers)
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
                    Id = command.SessionId,
                    Payload = payload
                };

                _Channel.Sender.Push(_serializer.Serialize(package));
            }
        }

        private void _HandleOutgoingData(OutgoingDataCommand command)
        {
            if (_sessionsById.TryGetValue(command.SessionId, out var session))
            {
                var payload = command.Payload;
                if (payload == null || payload.Length == 0)
                {
                    return;
                }

                var headerLength = PinionCore.Serialization.Varint.BufferToNumber(payload, 0, out int bodyLength);
                if (headerLength <= 0 || bodyLength <= 0 || payload.Length < headerLength + bodyLength)
                {
                    return;
                }

                var outbound = _pool.Alloc(bodyLength);
                Array.Copy(payload, headerLength, outbound.Bytes.Array, outbound.Bytes.Offset, bodyLength);
                session.Channel.Sender.Push(outbound);
            }
        }

        private List<Buffer> _ReadDone(List<Buffer> buffers)
        {
            foreach (var buffer in buffers)
            {
                var package = (ServerToClientPackage)_serializer.Deserialize(buffer);
                if (package.OpCode == OpCodeServerToClient.Message)
                {
                    _DataflowActor.Post(new OutgoingDataCommand { SessionId = package.Id, Payload = package.Payload });
                }
            }

            return new List<Buffer>();
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

            _DataflowActor.Complete();
            _DataflowActor.Dispose();
            _Channel.Dispose();

            foreach (var session in _streams.Values)
            {
                session.Dispose();
            }

            _streams.Clear();
            _sessionsById.Clear();
        }
    }
}
