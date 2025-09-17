using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using PinionCore.Memorys;
using PinionCore.Network;
using PinionCore.Remote.Actors;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway.Sessions
{
    struct ServerToClientPackage
    {
        public OpCodeServerToClient OpCode;
        public uint Id;
        public byte[] Payload;
    }

    class Serializer : PinionCore.Remote.Gateway.Serializer
    {
        public Serializer(IPool pool)
            : base(pool, new Type[]
            {
                typeof(ClientToServerPackage),
                typeof(ServerToClientPackage),
                typeof(OpCodeClientToServer),
                typeof(OpCodeServerToClient),
                typeof(uint),
                typeof(byte[]),
                typeof(byte)
            })
        {
        }
    }

    class GatewaySessionListener : IDisposable, IListenable
    {
        private readonly PackageReader _reader;
        private readonly PackageSender _sender;
        private readonly PinionCore.Remote.NotifiableCollection<IStreamable> _notifiableCollection;
        private readonly Dictionary<uint, SessionStream> _sessions;
        private readonly Serializer _serializer;
        private readonly DataflowActor<PackageEnvelope> _packageActor;
        private readonly CancellationTokenSource _cancellationSource;
        private readonly Task _processingTask;
        private bool _disposed;

        private sealed class PackageEnvelope
        {
            public PackageEnvelope(ClientToServerPackage package)
            {
                Package = package;
                Completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            public ClientToServerPackage Package { get; }

            public TaskCompletionSource<bool> Completion { get; }
        }

        private class SessionStream : IStreamable
        {
            private readonly Network.BufferRelay _incoming;
            private readonly uint _id;
            private readonly PackageSender _sender;
            private readonly Serializer _serializer;

            public uint Id
            {
                get { return _id; }
            }

            public SessionStream(uint id, PackageSender sender, Serializer serializer)
            {
                _id = id;
                _incoming = new Network.BufferRelay();
                _sender = sender;
                _serializer = serializer;
            }

            public void PushIncoming(byte[] buffer, int offset, int count)
            {
                _incoming.Push(buffer, offset, count);
            }

            Remote.IAwaitableSource<int> IStreamable.Receive(byte[] buffer, int offset, int count)
            {
                return _incoming.Pop(buffer, offset, count);
            }

            Remote.IAwaitableSource<int> IStreamable.Send(byte[] buffer, int offset, int count)
            {
                if (count <= 0)
                {
                    return new Network.NoWaitValue<int>(0);
                }

                var payload = new byte[count];
                Array.Copy(buffer, offset, payload, 0, count);

                var pkg = new ServerToClientPackage
                {
                    OpCode = OpCodeServerToClient.Message,
                    Id = _id,
                    Payload = payload
                };

                var segment = _serializer.Serialize(pkg);
                _sender.Push(segment);

                return new Network.NoWaitValue<int>(count);
            }
        }

        public GatewaySessionListener(PackageReader reader, PackageSender sender, Serializer serializer)
        {
            _reader = reader;
            _sender = sender;
            _serializer = serializer;

            _notifiableCollection = new PinionCore.Remote.NotifiableCollection<IStreamable>();
            _sessions = new Dictionary<uint, SessionStream>();
            _packageActor = new DataflowActor<PackageEnvelope>(HandlePackageAsync);
            _cancellationSource = new CancellationTokenSource();
            _processingTask = Task.Run(() => ProcessAsync(_cancellationSource.Token));
        }

        event Action<IStreamable> IListenable.StreamableEnterEvent
        {
            add { _notifiableCollection.Notifier.Supply += value; }
            remove { _notifiableCollection.Notifier.Supply -= value; }
        }

        event Action<IStreamable> IListenable.StreamableLeaveEvent
        {
            add { _notifiableCollection.Notifier.Unsupply += value; }
            remove { _notifiableCollection.Notifier.Unsupply -= value; }
        }

        private async Task ProcessAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var buffers = await _reader.Read().ConfigureAwait(false);
                    foreach (PinionCore.Memorys.Buffer buffer in buffers)
                    {
                        var pkg = (ClientToServerPackage)_serializer.Deserialize(buffer);

                        var envelope = new PackageEnvelope(pkg);
                        bool accepted;
                        try
                        {
                            accepted = await _packageActor.SendAsync(envelope, token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            envelope.Completion.TrySetCanceled();
                            throw;
                        }

                        if (!accepted)
                        {
                            envelope.Completion.TrySetCanceled();
                            throw new InvalidOperationException("Package actor declined processing.");
                        }

                        await envelope.Completion.Task.ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private Task HandlePackageAsync(PackageEnvelope envelope, CancellationToken token)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            try
            {
                token.ThrowIfCancellationRequested();
                ProcessPackage(envelope.Package);
                envelope.Completion.TrySetResult(true);
                return Task.CompletedTask;
            }
            catch (OperationCanceledException)
            {
                envelope.Completion.TrySetCanceled();
                throw;
            }
            catch (Exception ex)
            {
                envelope.Completion.TrySetException(ex);
                throw;
            }
        }

        private void ProcessPackage(ClientToServerPackage pkg)
        {
            switch (pkg.OpCode)
            {
                case OpCodeClientToServer.Join:
                    if (_sessions.ContainsKey(pkg.Id) == false)
                    {
                        var newSession = new SessionStream(pkg.Id, _sender, _serializer);
                        _sessions.Add(pkg.Id, newSession);
                        _notifiableCollection.Items.Add(newSession);
                    }
                    break;

                case OpCodeClientToServer.Message:
                    if (_sessions.TryGetValue(pkg.Id, out var session))
                    {
                        if (pkg.Payload != null && pkg.Payload.Length > 0)
                        {
                            session.PushIncoming(pkg.Payload, 0, pkg.Payload.Length);
                        }
                    }
                    break;

                case OpCodeClientToServer.Leave:
                    if (_sessions.TryGetValue(pkg.Id, out var existing))
                    {
                        _notifiableCollection.Items.Remove(existing);
                        _sessions.Remove(pkg.Id);
                    }
                    break;
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
                _processingTask.Wait(TimeSpan.FromSeconds(1));
            }
            catch (AggregateException ex)
            {
                ex.Handle(e => e is OperationCanceledException);
            }

            _packageActor.Dispose();
            _cancellationSource.Dispose();
            _sessions.Clear();
        }
    }
}
