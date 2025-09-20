namespace PinionCore.Remote.Gateway.Protocols
{
    public static partial class ProtocolProvider
    {
        public static PinionCore.Remote.IProtocol Create()
        {
            PinionCore.Remote.IProtocol protocol = null;
            _CreateCase1(ref protocol);
            return protocol;
        }

        [Remote.Protocol.Creator]
        static partial void _CreateCase1(ref PinionCore.Remote.IProtocol protocol);


    }
}

