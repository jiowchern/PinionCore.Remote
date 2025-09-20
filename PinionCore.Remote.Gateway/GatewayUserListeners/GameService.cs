using System.Net;
using System.Runtime.Serialization;
using PinionCore.Memorys;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway.GatewayUserListeners 
{
    class GameService : IService
    {
        System.Action _Dispose;

        public GameService(IEntry entry , IListenable listenable, IProtocol protocol , ISerializable serializable , IInternalSerializable internalSerializable  , IPool pool)
        {
            
            var service = new PinionCore.Remote.Soul.AsyncService(new SyncService(entry, protocol, serializable, internalSerializable, pool, listenable));
            _Dispose = () =>
            {
                var serviceDispose = service as System.IDisposable;
                serviceDispose.Dispose();                
            };
        }

        public void Dispose()
        {
            _Dispose();
        }

        public static IService Create(IEntry entry,GatewayUserListener listener)
        {
            var protocol = Protocols.ProtocolProvider.Create();

            return Create(entry, listener, protocol);
        }
        public static IService Create(IEntry entry, IListenable listenable, IProtocol protocol)
        {

            Memorys.Pool pool = PinionCore.Memorys.PoolProvider.Shared;
            var userProvider = new UserProvider(listenable, pool);
            return new GameService(entry, listenable, protocol, new PinionCore.Remote.Serializer(protocol.SerializeTypes), new PinionCore.Remote.InternalSerializer(), pool);
        }
    }
}
