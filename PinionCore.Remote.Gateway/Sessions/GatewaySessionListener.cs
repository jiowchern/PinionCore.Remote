using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

    class GatewaySessionListener : IDisposable, IListenable
    {
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
        readonly Channel _Channel;
        interface ActorCommand
        {
            
        }

        struct JoinCommand : ActorCommand
        {
            public uint Id;            
        }

        struct LeaveCommand : ActorCommand
        {
            public uint Id;
        }

        struct MessageCommand : ActorCommand
        {
            public uint Id;
            public byte[] Payload;
        }

        readonly DataflowActor<ActorCommand> DataflowActor_;
        private readonly PinionCore.Remote.NotifiableCollection<IStreamable> _notifiableCollection;
        private readonly Dictionary<uint, SessionStream> _sessions;
        private readonly Serializer _serializer;
        private bool _started;
        private bool _disposed;

        public GatewaySessionListener(PackageReader reader, PackageSender sender, Serializer serializer)
        {
            DataflowActor_ = new DataflowActor<ActorCommand>(_Handle);
            _Channel = new Channel(reader, sender);
            
            _serializer = serializer;

            _notifiableCollection = new PinionCore.Remote.NotifiableCollection<IStreamable>();
            _sessions = new Dictionary<uint, SessionStream>();            
        }

        private void _Handle(ActorCommand command)
        {
            switch (command)
            {
                case JoinCommand join:
                    if (_sessions.ContainsKey(join.Id) == false)
                    {
                        var newSession = new SessionStream(join.Id, _Channel.Sender, _serializer);
                        _sessions.Add(join.Id, newSession);
                        _notifiableCollection.Items.Add(newSession);
                    }
                    break;
                case LeaveCommand leave:
                    if (_sessions.TryGetValue(leave.Id, out var existing))
                    {
                        _notifiableCollection.Items.Remove(existing);
                        _sessions.Remove(leave.Id);
                    }
                    break;
                case MessageCommand message:
                    if (_sessions.TryGetValue(message.Id, out var session))
                    {
                        if (message.Payload != null && message.Payload.Length > 0)
                        {
                            session.PushIncoming(message.Payload, 0, message.Payload.Length);
                        }
                    }
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

        private List<Memorys.Buffer> _ReadDone(List<Memorys.Buffer> buffers)
        {
           
            foreach (Memorys.Buffer buffer in buffers)
            {
                var pkg = (ClientToServerPackage)_serializer.Deserialize(buffer);

                switch (pkg.OpCode)
                {
                    case OpCodeClientToServer.Join:
                        DataflowActor_.Post(new JoinCommand { Id = pkg.Id });
                        break;
                    case OpCodeClientToServer.Leave:
                        DataflowActor_.Post(new LeaveCommand { Id = pkg.Id });
                        break;
                    case OpCodeClientToServer.Message:
                        DataflowActor_.Post(new MessageCommand { Id = pkg.Id, Payload = pkg.Payload });
                        break;
                }

            }

            return new List<Memorys.Buffer>();
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

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(GatewaySessionListener));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            DataflowActor_.Complete();
            DataflowActor_.Dispose();
            _sessions.Clear();
        }
    }
}
