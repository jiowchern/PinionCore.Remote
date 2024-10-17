namespace PinionCore.Remote.Tools.Protocol.Sources.TestCommon
{
    public static partial class ProtocolProvider 
    {
        public static PinionCore.Remote.IProtocol CreateCase1()
        {
            PinionCore.Remote.IProtocol protocol = null;
            _CreateCase1(ref protocol);
            return protocol;
        }

        [Remote.Protocol.Creater]
        static partial void _CreateCase1(ref PinionCore.Remote.IProtocol protocol);


        public static IProtocol CreateCase2()
        {
            IProtocol protocol = null;
            _CreateCase2(ref protocol);
            return protocol;
        }

        [Remote.Protocol.Creater]
        static partial void _CreateCase2(ref IProtocol protocol);
    }
}
