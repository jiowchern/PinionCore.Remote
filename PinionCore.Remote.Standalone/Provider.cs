using PinionCore.Memorys;
using PinionCore.Network;
using PinionCore.Remote.Ghost;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Standalone
{
    public static class Provider
    {
        public static Service CreateService(IEntry entry, IProtocol protocol)
        {
            return CreateService(entry, protocol, new PinionCore.Remote.Serializer(protocol.SerializeTypes), new InternalSerializer(), PinionCore.Memorys.PoolProvider.Shared);
        }
        public static Service CreateService(IEntry entry, IProtocol protocol, ISerializable serializable, IInternalSerializable internalSerializable , PinionCore.Memorys.IPool pool)
        {
            return new Standalone.Service(entry, protocol, serializable, internalSerializable, pool);
        }

        public static IAgent CreateAgent(IProtocol protocol)
        {
            return CreateAgent(protocol, new PinionCore.Remote.Serializer(protocol.SerializeTypes), new PinionCore.Remote.InternalSerializer(), PinionCore.Memorys.PoolProvider.Shared);
        }

        public static IAgent CreateAgent(IProtocol protocol,ISerializable serializable , IInternalSerializable internalSerializable , IPool pool)
        {
            return new Ghost.Agent(protocol, serializable , internalSerializable, pool);
        }

        public static System.Action Connect(this IAgent agent ,IService service )
        {
            var stream = new PinionCore.Network.Stream();
            service.Join(stream);
            var agentStream = new PinionCore.Network.ReverseStream(stream);
            agent.Enable(agentStream);

            return () =>
            {
                agent.Disable();
                service.Leave(stream);              
            };
        }

        public static System.Action Connect(this IAgent agent, IStreamable stream)
        {
            agent.Enable(stream);
            return () =>
            {
                agent.Disable();            
            };
        }
    }
}
