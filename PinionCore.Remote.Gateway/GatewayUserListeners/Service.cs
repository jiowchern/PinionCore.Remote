using System;
using System.Net;
using System.Runtime.Serialization;
using PinionCore.Memorys;
using PinionCore.Network;
using PinionCore.Remote.Ghost;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway.GatewayUserListeners 
{
    
    
    class Service : IService
    {
        readonly System.Action _Dispose;
        readonly IService _GameService;
        readonly IService _UserService;

        public event Action<IStreamable> GameUserJoinEvent;
        public event Action<IStreamable> GameUserLeaveEvent;

        public Service(IEntry entry, IProtocol protocol)
            :this(entry, protocol, new PinionCore.Remote.Serializer(protocol.SerializeTypes), new PinionCore.Remote.InternalSerializer(), PinionCore.Memorys.PoolProvider.Shared)
        {

        }
        public Service(IEntry entry , IProtocol protocol , ISerializable serializable , IInternalSerializable internalSerializable  , IPool pool)
        {
            var userListener = new PinionCore.Remote.Gateway.GatewayUserListeners.GatewayUserListener();
            var userEntry = new Entry(userListener);
            var userProtocol = Protocols.ProtocolProvider.Create();
            _UserService = Standalone.Provider.CreateService(userEntry, userProtocol);
            _GameService = Standalone.Provider.CreateService(entry, protocol, serializable , internalSerializable , pool);


            IListenable listenable = userListener;
            listenable.StreamableLeaveEvent += _GameUserLeave;
            listenable.StreamableEnterEvent += _GameUserJoin;

            _Dispose = () =>
            {
                listenable.StreamableLeaveEvent -= _GameUserLeave;
                listenable.StreamableEnterEvent -= _GameUserJoin;
                _GameService.Dispose();
                _UserService.Dispose();                
            };
        }

        private void _GameUserJoin(IStreamable streamable)
        {
            _GameService.Join(streamable);
            GameUserJoinEvent?.Invoke(streamable);
        }

        private void _GameUserLeave(IStreamable streamable)
        {
            _GameService.Leave(streamable);
            GameUserLeaveEvent?.Invoke(streamable);
        }

        public void Dispose()
        {
            _Dispose();
        }

        void IService.Join(IStreamable user)
        {
            _UserService.Join(user);
        }

        void IService.Leave(IStreamable user)
        {
            _UserService.Leave(user);
        }
    }
}
