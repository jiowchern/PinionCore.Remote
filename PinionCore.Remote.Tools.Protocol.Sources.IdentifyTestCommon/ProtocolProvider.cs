namespace PinionCore.Remote.Tools.Protocol.Sources.IdentifyTestCommon
{
    public static partial class ProtocolProvider
    {
        public static PinionCore.Remote.IProtocol CreateCase1()
        {
            PinionCore.Remote.IProtocol protocol = null;
            _CreateCase1(ref protocol);
            return protocol;
        }

        [Remote.Protocol.Creator]
        static partial void _CreateCase1(ref PinionCore.Remote.IProtocol protocol);


    }
}


