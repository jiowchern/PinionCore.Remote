namespace PinionCore.Remote.Client
{

    public  class Provider
    {
    
        public static Remote.Ghost.User CreateAgent(IProtocol protocol, ISerializable serializable, PinionCore.Memorys.IPool pool)
        {
            return new Remote.Ghost.User(protocol, serializable, new PinionCore.Remote.InternalSerializer(), pool);
        }

        public static Remote.Ghost.User CreateAgent(IProtocol protocol)
        {
            return new Remote.Ghost.User(protocol, new PinionCore.Remote.Serializer(protocol.SerializeTypes), new PinionCore.Remote.InternalSerializer(), PinionCore.Memorys.PoolProvider.Shared);
        }

        public static TcpConnectSet CreateTcpAgent(IProtocol protocol, ISerializable serializable, PinionCore.Memorys.IPool pool)
        {
            var connecter = new PinionCore.Network.Tcp.Connector();

            Remote.Ghost.User agent = CreateAgent(protocol, serializable, pool);

            return new TcpConnectSet(connecter, agent);
        }

        public static TcpConnectSet CreateTcpAgent(IProtocol protocol)
        {
            var connecter = new PinionCore.Network.Tcp.Connector();
            Remote.Ghost.User agent = CreateAgent(protocol, new PinionCore.Remote.Serializer(protocol.SerializeTypes), PinionCore.Memorys.PoolProvider.Shared);
            return new TcpConnectSet(connecter, agent);
        }
    }
}
