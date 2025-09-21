using PinionCore.Remote.Ghost;

namespace PinionCore.Remote.Gateway
{
    internal static class Provider
    {
        public static IAgent CreateAgent()
        {
            var protocol = Protocols.ProtocolProvider.Create();
            return Standalone.Provider.CreateAgent(protocol);
        }
    }
}
