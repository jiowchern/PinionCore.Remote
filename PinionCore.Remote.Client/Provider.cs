using PinionCore.Network;
using PinionCore.Remote.Ghost;
using System;

namespace PinionCore.Remote.Client
{

    public class Provider
    {        
        public static Ghost.Agent CreateAgent(IProtocol protocol, ISerializable serializable , PinionCore.Memorys.IPool pool)
        {
            return new Ghost.Agent(protocol, serializable , new PinionCore.Remote.InternalSerializer(), pool);
        }

        public static Ghost.Agent CreateAgent(IProtocol protocol)
        {
            return new Ghost.Agent( protocol, new PinionCore.Remote.Serializer(protocol.SerializeTypes) , new PinionCore.Remote.InternalSerializer() , PinionCore.Memorys.PoolProvider.Shared);
        }

        public static TcpConnectSet CreateTcpAgent(IProtocol protocol, ISerializable serializable, PinionCore.Memorys.IPool pool)
        {
            var connecter = new PinionCore.Network.Tcp.Connector();
            
            var agent = CreateAgent(protocol, serializable , pool);
            
            return new TcpConnectSet(connecter, agent);
        }

        public static TcpConnectSet CreateTcpAgent(IProtocol protocol)
        {
            var connecter = new PinionCore.Network.Tcp.Connector();
            var agent = CreateAgent(protocol,  new PinionCore.Remote.Serializer(protocol.SerializeTypes), PinionCore.Memorys.PoolProvider.Shared);            
            return new TcpConnectSet(connecter, agent);
        }
    }
}