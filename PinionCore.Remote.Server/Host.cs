namespace PinionCore.Remote.Server
{
    public class Host : Remote.Soul.Service
    {
        public Host(IEntry entry, IProtocol protocol)
            : base(entry, protocol, new PinionCore.Remote.Serializer(protocol.SerializeTypes), new PinionCore.Remote.InternalSerializer(), PinionCore.Memorys.PoolProvider.Shared)
        {
        }        
    }
}
