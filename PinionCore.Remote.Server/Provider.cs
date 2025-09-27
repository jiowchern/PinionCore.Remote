namespace PinionCore.Remote.Server
{
    public static class Provider
    {
        public static Soul.IService CreateService(IEntry entry, IProtocol protocol, Soul.IListenable listenable)
        {
            Memorys.Pool pool = PinionCore.Memorys.PoolProvider.Shared;            
            return new Soul.AsyncService(new Soul.SyncService(entry, protocol, new PinionCore.Remote.Serializer(protocol.SerializeTypes), new PinionCore.Remote.InternalSerializer(), pool));
        }

        public static Soul.IService CreateService(IEntry entry, IProtocol protocol, ISerializable serializable)
        {
            Memorys.Pool pool = PinionCore.Memorys.PoolProvider.Shared;            
            return new Soul.AsyncService(new Soul.SyncService(entry, protocol, serializable, new PinionCore.Remote.InternalSerializer(), pool));
        }

        public static TcpListenSet CreateTcpService(IEntry entry, IProtocol protocol)
        {
            return CreateTcpService(entry, protocol, new PinionCore.Remote.Serializer(protocol.SerializeTypes));
        }
        public static TcpListenSet CreateTcpService(IEntry entry, IProtocol protocol, ISerializable serializable)
        {
            var listener = new PinionCore.Remote.Server.Tcp.Listener();
            Soul.IService service = CreateService(entry, protocol, serializable);
            return new TcpListenSet(listener, service);
        }
        public static WebListenSet CreateWebService(IEntry entry, IProtocol protocol)
        {
            return CreateWebService(entry, protocol, new PinionCore.Remote.Serializer(protocol.SerializeTypes));
        }
        public static WebListenSet CreateWebService(IEntry entry, IProtocol protocol, ISerializable serializable)
        {
            var listener = new PinionCore.Remote.Server.Web.Listener();
            Soul.IService service = CreateService(entry, protocol, serializable);
            return new WebListenSet(listener, service);
        }
    }
}
