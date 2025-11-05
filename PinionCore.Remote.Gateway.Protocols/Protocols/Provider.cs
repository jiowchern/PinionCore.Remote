using PinionCore.Extensions;
using PinionCore.Remote.Ghost;

namespace PinionCore.Remote.Gateway.Protocols
{
    internal static class Provider
    {
        public static IAgent ToAgent(this IProtocol protocol)
        {            
            PinionCore.Utility.Log.Instance.WriteInfo($"Creating agent with protocol: {protocol.VersionCode.ToMd5String()}");
            return new PinionCore.Remote.Ghost.Agent(protocol);
        }
    }
}
