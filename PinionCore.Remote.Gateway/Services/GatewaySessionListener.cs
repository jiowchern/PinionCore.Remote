using System;
using PinionCore.Memorys;
using PinionCore.Network;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway.Services
{
    enum OpCodeClientToServer : byte
    {
        None = 0,
        Join = 1,
        Leave = 2,
        Message = 3
    }

    enum OpCodeServerToClient : byte
    {
        None = 0,        
        Message = 1
    }
    struct ClientToServerPackage
    {
        public OpCodeClientToServer OpCode;
        public uint Id;
        public byte[] Payload;
    }

    struct ServerToClientPackage
    {
        public OpCodeServerToClient OpCode;
        public uint Id;
        public byte[] Payload;
    }

    class Serializer : PinionCore.Remote.Gateway.Serializer
    {
        public Serializer(IPool pool) : base(pool, new Type[] {
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

    public class GatewaySessionListener : System.IDisposable ,IListenable
    {
        readonly PackageReader _Reader;
        readonly PackageSender _Sender;

        readonly PinionCore.Remote.NotifiableCollection<IStreamable> _NotifiableCollection;
        readonly System.Collections.Generic.Dictionary<uint, SessionStream> _Sessions;
        readonly Serializer _Serializer;

        private System.Runtime.CompilerServices.TaskAwaiter<System.Collections.Generic.List<PinionCore.Memorys.Buffer>> _ReadTask;
        private bool _Started;

        private class SessionStream : IStreamable
        {
            private readonly Network.BufferRelay _Incoming; // data coming from remote client
            private readonly uint _Id;
            private readonly PackageSender _Sender;
            private readonly Serializer _Serializer;
            public uint Id { get { return _Id; } }

            public SessionStream(uint id, PackageSender sender, Serializer serializer)
            {
                _Id = id;
                _Incoming = new Network.BufferRelay();
                _Sender = sender;
                _Serializer = serializer;
            }

            public void PushIncoming(byte[] buffer, int offset, int count)
            {
                _Incoming.Push(buffer, offset, count);
            }

            Remote.IAwaitableSource<int> IStreamable.Receive(byte[] buffer, int offset, int count)
            {
                return _Incoming.Pop(buffer, offset, count);
            }

            Remote.IAwaitableSource<int> IStreamable.Send(byte[] buffer, int offset, int count)
            {
                if (count <= 0)
                    return new Network.NoWaitValue<int>(0);

                var payload = new byte[count];
                Array.Copy(buffer, offset, payload, 0, count);

                var pkg = new ServerToClientPackage
                {
                    OpCode = OpCodeServerToClient.Message,
                    Id = _Id,
                    Payload = payload
                };
                var seg = _Serializer.Serialize(pkg);
                _Sender.Push(seg);

                return new Network.NoWaitValue<int>(count);
            }
        }

        public GatewaySessionListener(PackageReader reader, PackageSender sender)
        {
            _Reader = reader;
            _Sender = sender;

            _NotifiableCollection = new PinionCore.Remote.NotifiableCollection<IStreamable>();
            _Sessions = new System.Collections.Generic.Dictionary<uint, SessionStream>();
            _Serializer = new Serializer(PinionCore.Memorys.PoolProvider.Shared);

            _Started = false;
        }

        event Action<IStreamable> IListenable.StreamableEnterEvent
        {
            add { _NotifiableCollection.Notifier.Supply += value; }
            remove { _NotifiableCollection.Notifier.Supply -= value; }
        }

        event Action<IStreamable> IListenable.StreamableLeaveEvent
        {
            add { _NotifiableCollection.Notifier.Unsupply += value; }
            remove { _NotifiableCollection.Notifier.Unsupply -= value; }
        }

        private void _StartRead()
        {
            _ReadTask = _Reader.Read().GetAwaiter();
            _Started = true;
        }

        internal void HandlePackages()
        {
            if (_Started == false)
            {
                _StartRead();
            }

            if (_ReadTask.IsCompleted)
            {
                System.Collections.Generic.List<PinionCore.Memorys.Buffer> buffers = _ReadTask.GetResult();

                foreach (PinionCore.Memorys.Buffer buffer in buffers)
                {
                    var obj = _Serializer.Deserialize(buffer);
                    var pkg = (ClientToServerPackage)obj;

                    if (pkg.OpCode == OpCodeClientToServer.Join)
                    {
                        if (_Sessions.ContainsKey(pkg.Id) == false)
                        {
                            var session = new SessionStream(pkg.Id, _Sender, _Serializer);
                            _Sessions.Add(pkg.Id, session);
                            _NotifiableCollection.Items.Add(session);
                        }
                    }
                    else if (pkg.OpCode == OpCodeClientToServer.Message)
                    {
                        SessionStream session;
                        if (_Sessions.TryGetValue(pkg.Id, out session))
                        {
                            if (pkg.Payload != null && pkg.Payload.Length > 0)
                            {
                                session.PushIncoming(pkg.Payload, 0, pkg.Payload.Length);
                            }
                        }
                    }
                    else if (pkg.OpCode == OpCodeClientToServer.Leave)
                    {
                        SessionStream session;
                        if (_Sessions.TryGetValue(pkg.Id, out session))
                        {
                            _NotifiableCollection.Items.Remove(session);
                            _Sessions.Remove(pkg.Id);
                        }
                    }
                }

                _ReadTask = _Reader.Read().GetAwaiter();
            }
        }

        void System.IDisposable.Dispose()
        {
            // No unmanaged resources; clear state
            _Sessions.Clear();
        }

    }
    
}
