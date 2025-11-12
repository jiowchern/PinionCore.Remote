namespace PinionCore.Remote.Server
{
    public class Soul : Remote.Soul.Service
    {
        public Soul(IEntry entry, IProtocol protocol)
            : base(entry, protocol, new PinionCore.Remote.Serializer(protocol.SerializeTypes), new PinionCore.Remote.InternalSerializer(), PinionCore.Memorys.PoolProvider.Shared)
        {
        }        
    }
}
