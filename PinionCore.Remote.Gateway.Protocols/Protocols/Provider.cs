using PinionCore.Extensions;
using PinionCore.Remote.Ghost;

namespace PinionCore.Remote.Gateway.Protocols
{
    internal static class Provider
    {
        public static IAgent CreateAgent()
        {
            var protocol = ProtocolProvider.Create();
            PinionCore.Utility.Log.Instance.WriteInfo($"Creating agent with protocol: {protocol.VersionCode.ToMd5String()}");

            return Standalone.Provider.CreateAgent(protocol);
        }
    }
}
