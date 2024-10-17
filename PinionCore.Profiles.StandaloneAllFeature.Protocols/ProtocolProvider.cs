using System;

namespace PinionCore.Profiles.StandaloneAllFeature.Protocols
{
    public static partial class ProtocolProvider
    {
        public static PinionCore.Remote.IProtocol Create()
        {
            PinionCore.Remote.IProtocol protocol = null;
            _Create(ref protocol);
            return protocol;
        }

        [Remote.Protocol.Creater]
        static partial void _Create(ref PinionCore.Remote.IProtocol protocol);


    }
}
