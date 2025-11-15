using System;
using System.Threading.Tasks;
using PinionCore.Remote.Ghost;

namespace PinionCore.Remote.Client
{
    public static class AgentExtensions
    {
        public static async Task<IDisposable> Connect(this Proxy guest, IConnectingEndpoint endpoint)
        {
            return await guest.Agent.Connect(endpoint);
        }
        public static async Task<IDisposable> Connect(this IAgent agent, IConnectingEndpoint endpoint)
        {
            var stream = await endpoint.ConnectAsync();
            agent.Enable(stream);

            return new Utility.DisposeAction(() =>
            {
                agent.Disable();
                endpoint.Dispose();
            });
        }
    }
}
