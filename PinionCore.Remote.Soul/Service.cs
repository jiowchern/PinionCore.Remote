using PinionCore.Memorys;
using PinionCore.Network;


namespace PinionCore.Remote.Soul
{
    public class Service : Soul.IService
    {
        readonly PinionCore.Remote.Soul.IService _Service;

        internal readonly IProtocol Protocol;
        internal readonly ISerializable Serializer;
        private readonly IPool _Pool;
        

        public Service(IEntry entry, IProtocol protocol)
            : this(entry, protocol, new PinionCore.Remote.Serializer(protocol.SerializeTypes), new PinionCore.Remote.InternalSerializer(), PinionCore.Memorys.PoolProvider.Shared)
        {
        }
        public Service(IEntry entry, IProtocol protocol, ISerializable serializable, PinionCore.Remote.IInternalSerializable internal_serializable, Memorys.IPool pool)
        {
            _Pool = pool;        
            Protocol = protocol;
            Serializer = serializable;            
            var service = new PinionCore.Remote.Soul.AsyncService(new SyncService(entry, protocol, serializable, internal_serializable, _Pool));
            _Service = service;            
        }


        public void Dispose()
        {
            _Service.Dispose();
        }

        void IService.Join(IStreamable user)
        {
            _Service.Join(user);
            
        }

        void IService.Leave(IStreamable user)
        {
            _Service.Leave(user);
        }

        
    }
}
