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
        
        

        private Task _readTask;
        private bool _started;
        private bool _disposed;

      
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

        private void _ReadDone(Task<List<Memorys.Buffer>> task)
        {
            if (task.IsFaulted)
            {
                // todo : 如果失敗代表連線中斷 需要釋放 this
                return;
            }
            var buffers = task.Result;
            if (buffers == null || buffers.Count == 0)
            {
                //todo : 如果為空代表連線中斷 需要釋放 this
                return;
            }
            foreach (Memorys.Buffer buffer in buffers)
            {
                var pkg = (ClientToServerPackage)_serializer.Deserialize(buffer);

                ProcessPackage(pkg);
            }

            if (!_disposed)
            {
                _readTask = _reader.Read().ContinueWith(_ReadDone);
            }
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

            
            try
            {
                _readTask?.Wait(TimeSpan.FromSeconds(1));
            }
            catch (AggregateException ex)
            {
                ex.Handle(e => e is OperationCanceledException);
            }

            
            
            _sessions.Clear();
        }
    }
}
