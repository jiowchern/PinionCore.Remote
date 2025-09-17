using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PinionCore.Memorys;
using PinionCore.Network;
using PinionCore.Remote.Actors;

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
        private readonly DataflowActor<ConnectorMessage> _messageActor;
        private readonly CancellationTokenSource _cancellationSource;
        private readonly Task _processingTask;
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

        private abstract class ConnectorMessage
        {
            protected ConnectorMessage()
            {
                Completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            public TaskCompletionSource<bool> Completion { get; }
        }

        private sealed class JoinMessage : ConnectorMessage
        {
            public JoinMessage(IStreamable stream)
            {
                Stream = stream ?? throw new ArgumentNullException(nameof(stream));
                SessionCompletion = new TaskCompletionSource<uint>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            public IStreamable Stream { get; }
            public TaskCompletionSource<uint> SessionCompletion { get; }
        }

        private sealed class LeaveMessage : ConnectorMessage
        {
            public LeaveMessage(IStreamable stream)
            {
                Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            }

            public IStreamable Stream { get; }
        }

        private sealed class ClientPackageMessage : ConnectorMessage
        {
            public ClientPackageMessage(ClientToServerPackage package)
            {
                Package = package;
            }

            public ClientToServerPackage Package { get; }
        }

        private sealed class ServerPackageMessage : ConnectorMessage
        {
            public ServerPackageMessage(ServerToClientPackage package)
            {
                Package = package;
            }

            public ServerToClientPackage Package { get; }
        }

        public GatewaySessionConnector(PackageReader reader, PackageSender sender, Serializer serializer)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

            _streams = new Dictionary<IStreamable, Session>();
            _sessionsById = new Dictionary<uint, Session>();
            _cancellationSource = new CancellationTokenSource();
            _messageActor = new DataflowActor<ConnectorMessage>(HandleMessageAsync, new ActorOptions
            {
                DisposeTimeout = TimeSpan.FromSeconds(1)
            });

            _processingTask = Task.Run(() => ProcessIncomingPackagesAsync(_cancellationSource.Token));
        }

        public uint Join(IStreamable stream)
        {
            ThrowIfDisposed();

            var message = new JoinMessage(stream);
            SendMessage(message);

            return message.SessionCompletion.Task.GetAwaiter().GetResult();
        }

        public void Leave(IStreamable stream)
        {
            ThrowIfDisposed();

            var message = new LeaveMessage(stream);
            SendMessage(message);
        }

        private void SendMessage(ConnectorMessage message)
        {
            bool accepted;
            try
            {
                accepted = _messageActor.SendAsync(message, _cancellationSource.Token).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                message.Completion.TrySetCanceled();
                throw;
            }

            if (!accepted)
            {
                message.Completion.TrySetCanceled();
                throw new InvalidOperationException("Connector actor declined processing.");
            }

            message.Completion.Task.GetAwaiter().GetResult();
        }

        private async Task HandleMessageAsync(ConnectorMessage message, CancellationToken token)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            switch (message)
            {
                case JoinMessage joinMessage:
                    await HandleJoinAsync(joinMessage, token).ConfigureAwait(false);
                    break;
                case LeaveMessage leaveMessage:
                    HandleLeave(leaveMessage, token);
                    break;
                case ClientPackageMessage clientPackage:
                    HandleClientPackage(clientPackage, token);
                    break;
                case ServerPackageMessage serverPackage:
                    HandleServerPackage(serverPackage, token);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported connector message type {message.GetType().FullName}.");
            }
        }

        private Task HandleJoinAsync(JoinMessage message, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                if (_streams.TryGetValue(message.Stream, out var existing))
                {
                    message.SessionCompletion.TrySetResult(existing.Id);
                    message.Completion.TrySetResult(true);
                    return Task.CompletedTask;
                }

                var id = (uint)Interlocked.Increment(ref _GlobalSessionId);
                var sessionCancellation = CancellationTokenSource.CreateLinkedTokenSource(_cancellationSource.Token);
                var session = new Session(id, message.Stream, sessionCancellation);

                _streams.Add(message.Stream, session);
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

                message.SessionCompletion.TrySetResult(id);
                message.Completion.TrySetResult(true);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                message.SessionCompletion.TrySetException(ex);
                message.Completion.TrySetException(ex);
                throw;
            }
        }

        private void HandleLeave(LeaveMessage message, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                if (!_streams.TryGetValue(message.Stream, out var session))
                {
                    message.Completion.TrySetResult(true);
                    return;
                }

                _streams.Remove(message.Stream);
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

                message.Completion.TrySetResult(true);
            }
            catch (Exception ex)
            {
                message.Completion.TrySetException(ex);
                throw;
            }
        }

        private void HandleClientPackage(ClientPackageMessage message, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                var buffer = _serializer.Serialize(message.Package);
                _sender.Push(buffer);

                message.Completion.TrySetResult(true);
            }
            catch (Exception ex)
            {
                message.Completion.TrySetException(ex);
                throw;
            }
        }

        private void HandleServerPackage(ServerPackageMessage message, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                if (message.Package.OpCode == OpCodeServerToClient.Message &&
                    _sessionsById.TryGetValue(message.Package.Id, out var session))
                {
                    var payload = message.Package.Payload ?? Array.Empty<byte>();
                    session.Stream.Send(payload, 0, payload.Length);
                }

                message.Completion.TrySetResult(true);
            }
            catch (Exception ex)
            {
                message.Completion.TrySetException(ex);
                throw;
            }
        }

        private async Task ProcessIncomingPackagesAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var buffers = await _reader.Read().ConfigureAwait(false);
                    foreach (PinionCore.Memorys.Buffer buffer in buffers)
                    {
                        var package = (ServerToClientPackage)_serializer.Deserialize(buffer);
                        var message = new ServerPackageMessage(package);
                        bool accepted;

                        try
                        {
                            accepted = await _messageActor.SendAsync(message, token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            message.Completion.TrySetCanceled();
                            throw;
                        }

                        if (!accepted)
                        {
                            message.Completion.TrySetCanceled();
                            throw new InvalidOperationException("Connector actor declined processing.");
                        }

                        await message.Completion.Task.ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
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

                    var message = new ClientPackageMessage(package);
                    bool accepted;
                    try
                    {
                        accepted = await _messageActor.SendAsync(message, token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        message.Completion.TrySetCanceled();
                        throw;
                    }

                    if (!accepted)
                    {
                        message.Completion.TrySetCanceled();
                        throw new InvalidOperationException("Connector actor declined processing.");
                    }

                    await message.Completion.Task.ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _messageActor.Fault(ex);
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
                _processingTask?.Wait(TimeSpan.FromSeconds(1));
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

            _messageActor.Cancel();
            _messageActor.Dispose();
            _cancellationSource.Dispose();
            _streams.Clear();
            _sessionsById.Clear();
        }
    }
}
