using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PinionCore.Memorys;
using PinionCore.Network;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway
{

    class ServiceRegistrySerializer : Serializer
    {
        public ServiceRegistrySerializer(IPool pool) : base(pool, new Type[] {
            typeof(ServiceRegistryPackage),
            typeof(OpCodeFromServiceRegistry),
            typeof(SessionListenerPackage),
            typeof(OpCodeFromSessionListener),
            typeof(byte[]),
            typeof(uint),
            typeof(byte)
        })
        {
        }
    }



    public class SessionListener : PinionCore.Remote.Soul.IListenable , IDisposable
    {
        class User : IDisposable
        {
            public readonly IStreamable UserStream;
            readonly Stream _Stream;
            readonly Channel _Channel;
            private uint _UserId;
            private Task _ReadTask;
            bool _Disposed;
            public User(uint userId , IPool pool)
            {
                _Stream = new Stream();
                _Channel = new Channel(new PackageReader(_Stream, pool), new PackageSender(_Stream, pool));
                _Channel.OnDataReceived += _ReadDone;
                UserStream = _Stream;
                _UserId = userId;
                _ReadTask = Task.CompletedTask;
                
            }

            public void Start()
            {
                
                _Channel.Start();
            }

            List<Memorys.Buffer> _ReadDone(List<Memorys.Buffer> buffers)
            {
                if (_Disposed)
                    return new List<Memorys.Buffer>();
                OnDataReceived.Invoke(_UserId, buffers);
                return new List<Memorys.Buffer>();
            }

            public event System.Action<uint, List<Memorys.Buffer>> OnDataReceived;
            internal void FromGatewayPush(Memorys.Buffer buffer)
            {
                _Channel.Sender.Push(buffer);
            }

            public void Dispose()
            {
                _Channel.OnDataReceived -= _ReadDone;
                _Disposed = true;
            }
        }
        readonly System.Collections.Concurrent.ConcurrentDictionary<uint, User> _UserDict ;
        readonly IStreamable _Gateway;
        readonly Channel _Channel;
        readonly IPool _Pool;
        readonly Serializer _Serializer;
        // stream 是來自與 gateway 的連線
        public SessionListener(IStreamable stream , IPool pool, Serializer serializer)
        {
            _UserDict = new System.Collections.Concurrent.ConcurrentDictionary<uint, User>();
            _Pool = pool;
            _Gateway = stream;
            _Serializer = serializer;
            _Channel = new Channel(new PackageReader(_Gateway, pool), new PackageSender(_Gateway, pool));            
        }

        private List<Memorys.Buffer> _DataReceived(List<Memorys.Buffer> buffers)
        {
            foreach (var buffer in buffers)
            {
                var pkg = (ServiceRegistryPackage)_Serializer.Deserialize(buffer);
                var userId = pkg.UserId;
                User user = null;


                _UserDict.TryGetValue(userId, out user);

                if (pkg.OpCode == OpCodeFromServiceRegistry.Message && user != null)
                {
                    var buf = _Pool.Alloc(pkg.Payload.Length);
                    Array.Copy(pkg.Payload, 0, buf.Bytes.Array, buf.Bytes.Offset, pkg.Payload.Length);
                    user.FromGatewayPush(buf);
                } else if (pkg.OpCode == OpCodeFromServiceRegistry.Join && user == null)
                {
                    user = new User(userId , _Pool);
                    user.OnDataReceived += _DataReceived;
                    user.Start();
                    _UserDict.TryAdd(userId, user);
                    _StreamableEnterEvent(user.UserStream);

                }
                else if (pkg.OpCode == OpCodeFromServiceRegistry.Leave && user != null)
                {
                    _UserDict.TryRemove(userId, out user);
                    user.OnDataReceived -= _DataReceived;
                    user.Dispose();
                    _StreamableLeaveEvent(user.UserStream);
                }

            }
            return new List<Memorys.Buffer>();
        }

        private void _DataReceived(uint user, List<Memorys.Buffer> buffers)
        {
            foreach (var buffer in buffers)
            {
                var payload = new byte[buffer.Bytes.Count];
                Array.Copy(buffer.Bytes.Array, buffer.Bytes.Offset, payload, 0, buffer.Bytes.Count);
                var pkg = new SessionListenerPackage()
                {
                    OpCode = OpCodeFromSessionListener.Message,
                    UserId = user,
                    Payload = payload
                };
                var sendBuffer = _Serializer.Serialize(pkg);
                _Channel.Sender.Push(sendBuffer);
            }
        }

        public event Action OnDisconnected;

        public void Start()
        {
            _Channel.OnDisconnected += OnDisconnected;
            _Channel.OnDataReceived += _DataReceived;
            _Channel.Start();
        }


        event Action<IStreamable> _StreamableEnterEvent;
        event Action<IStreamable> IListenable.StreamableEnterEvent
        {
            add
            {
                _StreamableEnterEvent+= value;
            }

            remove
            {
                _StreamableEnterEvent -= value;
            }
        }

        event Action<IStreamable> _StreamableLeaveEvent;
        event Action<IStreamable> IListenable.StreamableLeaveEvent
        {
            add
            {
                _StreamableLeaveEvent += value;
            }

            remove
            {
                _StreamableLeaveEvent -= value;
            }
        }

        public void Dispose()
        {
            _Channel.OnDisconnected -= OnDisconnected;
            _Channel.OnDataReceived -= _DataReceived;
            _Channel.Dispose();
        }
    }
}
