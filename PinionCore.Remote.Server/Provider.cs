namespace PinionCore.Remote.Server
{
    public static class Provider
    {
        public static Soul.IService CreateService(IEntry entry, IProtocol protocol, Soul.IListenable listenable)
        {
            Memorys.Pool pool = PinionCore.Memorys.PoolProvider.Shared;
            var userProvider = new Soul.UserProvider(listenable, pool);
            return new Soul.AsyncService(new Soul.SyncService(entry, protocol, new PinionCore.Remote.Serializer(protocol.SerializeTypes), new PinionCore.Remote.InternalSerializer(), pool, userProvider));
        }

        public static Soul.IService CreateService(IEntry entry, IProtocol protocol, ISerializable serializable, Soul.IListenable listenable)
        {
            Memorys.Pool pool = PinionCore.Memorys.PoolProvider.Shared;
            var userProvider = new Soul.UserProvider(listenable, pool);
            return new Soul.AsyncService(new Soul.SyncService(entry, protocol, serializable, new PinionCore.Remote.InternalSerializer(), pool, userProvider));
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
