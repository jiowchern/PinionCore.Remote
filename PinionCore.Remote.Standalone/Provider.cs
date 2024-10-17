namespace PinionCore.Remote.Standalone
{
    public static class Provider
    {
        public static Service CreateService(IEntry entry, IProtocol protocol)
        {

            return CreateService(entry, protocol, new PinionCore.Remote.Serializer(protocol.SerializeTypes),PinionCore.Memorys.PoolProvider.Shared );
        }
        public static Service CreateService(IEntry entry , IProtocol protocol, ISerializable serializable ,PinionCore.Memorys.IPool pool)
        {
            return new Standalone.Service(entry, protocol, serializable , new PinionCore.Remote.InternalSerializer(), pool);
        }

       
    }
}
