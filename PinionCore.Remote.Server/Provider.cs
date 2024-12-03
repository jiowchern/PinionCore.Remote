using System.Runtime.Serialization;

namespace PinionCore.Remote.Server
{


    public static class Provider
    {


        public static Soul.IService CreateService(IEntry entry, IProtocol protocol, Soul.IListenable listenable)
        {
            Memorys.Pool pool = PinionCore.Memorys.PoolProvider.Shared;
            return new Soul.AsyncService(new Soul.SyncService(entry, new Soul.UserProvider(protocol, new PinionCore.Remote.Serializer(protocol.SerializeTypes), listenable, new PinionCore.Remote.InternalSerializer(), pool)));
        }

        public static Soul.IService CreateService(IEntry entry, IProtocol protocol, ISerializable serializable, Soul.IListenable listenable)
        {
            Memorys.Pool pool = PinionCore.Memorys.PoolProvider.Shared;

            return new Soul.AsyncService(new Soul.SyncService(entry, new Soul.UserProvider(protocol, serializable, listenable, new PinionCore.Remote.InternalSerializer(), pool)));
        }

        public static TcpListenSet CreateTcpService(IEntry entry, IProtocol protocol)
        {
            return CreateTcpService(entry, protocol, new PinionCore.Remote.Serializer(protocol.SerializeTypes));
        }
        public static TcpListenSet CreateTcpService(IEntry entry, IProtocol protocol, ISerializable serializable)
        {
            var listener = new PinionCore.Remote.Server.Tcp.Listener();
            Soul.IService service = CreateService(entry, protocol, serializable, listener);
            return new TcpListenSet(listener, service);
        }
        public static WebListenSet CreateWebService(IEntry entry, IProtocol protocol)
        {
            return CreateWebService(entry, protocol, new PinionCore.Remote.Serializer(protocol.SerializeTypes));
        }
        public static WebListenSet CreateWebService(IEntry entry, IProtocol protocol, ISerializable serializable)
        {
            var listener = new PinionCore.Remote.Server.Web.Listener();
            Soul.IService service = CreateService(entry, protocol, serializable, listener);
            return new WebListenSet(listener, service);
        }
      
       
    }
}
