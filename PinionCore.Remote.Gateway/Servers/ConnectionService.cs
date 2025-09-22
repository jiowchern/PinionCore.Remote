using System;
using System.Net;
using System.Runtime.Serialization;
using PinionCore.Memorys;
using PinionCore.Network;
using PinionCore.Remote.Ghost;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway.Servers 
{
    
    
    class GatewayService : IService
    {
        readonly System.Action _Dispose;
        readonly IService _GameService;
        readonly IService _ClientService;

        public GatewayService(IEntry entry, IProtocol protocol)
            :this(entry, protocol, new PinionCore.Remote.Serializer(protocol.SerializeTypes), new PinionCore.Remote.InternalSerializer(), PinionCore.Memorys.PoolProvider.Shared)
        {

        }
        public GatewayService(IEntry entry , IProtocol protocol , ISerializable serializable , IInternalSerializable internalSerializable  , IPool pool)
        {
            var clientListener = new PinionCore.Remote.Gateway.Servers.ConnectionListener();
            var clientEntry = new ServiceEntryPoint(clientListener);
            var clientProtocol = Protocols.ProtocolProvider.Create();
            _ClientService = Standalone.Provider.CreateService(clientEntry, clientProtocol);
            _GameService = Standalone.Provider.CreateService(entry, protocol, serializable , internalSerializable , pool);


            IListenable listenable = clientListener;
            listenable.StreamableLeaveEvent += _GameUserLeave;
            listenable.StreamableEnterEvent += _GameUserJoin;

            _Dispose = () =>
            {
                listenable.StreamableLeaveEvent -= _GameUserLeave;
                listenable.StreamableEnterEvent -= _GameUserJoin;
                _GameService.Dispose();
                _ClientService.Dispose();                
            };
        }

        private void _GameUserJoin(IStreamable streamable)
        {
            _GameService.Join(streamable);

        }

        private void _GameUserLeave(IStreamable streamable)
        {
            _GameService.Leave(streamable);

        }

        public void Dispose()
        {
            _Dispose();
        }

        void IService.Join(IStreamable user)
        {
            _ClientService.Join(user);
        }

        void IService.Leave(IStreamable user)
        {
            _ClientService.Leave(user);
        }
    }
}
