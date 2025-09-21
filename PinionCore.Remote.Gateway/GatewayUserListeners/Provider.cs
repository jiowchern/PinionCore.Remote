using PinionCore.Remote.Ghost;

namespace PinionCore.Remote.Gateway.GatewayUserListeners
{
    internal static class Provider
    {
        public static IAgent CreateAgent()
        {
            var protocol = PinionCore.Remote.Gateway.Protocols.ProtocolProvider.Create();
            return PinionCore.Remote.Standalone.Provider.CreateAgent(protocol);
        }
    }
}
