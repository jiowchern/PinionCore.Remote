using PinionCore.Remote.Ghost;

namespace PinionCore.Remote.Gateway.Protocols
{
    internal static class Provider
    {
        public static IAgent CreateAgent()
        {
            var protocol = ProtocolProvider.Create();
            return Standalone.Provider.CreateAgent(protocol);
        }
    }
}
