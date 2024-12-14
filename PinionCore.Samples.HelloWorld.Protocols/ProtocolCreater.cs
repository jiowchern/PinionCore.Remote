namespace PinionCore.Samples.HelloWorld.Protocols
{
    public static partial class ProtocolCreater
    {
        public static PinionCore.Remote.IProtocol Create()
        {
            PinionCore.Remote.IProtocol p = null;
            _Create(ref p);
            return p;
        }

        [PinionCore.Remote.Protocol.Creater]
        static partial void _Create(ref PinionCore.Remote.IProtocol p);
    }
}
