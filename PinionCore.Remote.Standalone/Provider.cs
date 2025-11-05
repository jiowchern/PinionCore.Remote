using PinionCore.Memorys;
using PinionCore.Network;
using PinionCore.Remote.Ghost;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Standalone
{
    public static class Provider
    {

        public static System.Action Connect(this IAgent agent ,IService service )
        {
            var stream = new PinionCore.Network.Stream();
            service.Join(stream);
            var agentStream = new PinionCore.Network.ReverseStream(stream);
            agent.Enable(agentStream);

            return () =>
            {
                agent.Disable();
                service.Leave(stream);              
            };
        }

        public static System.Action Connect(this IAgent agent, IStreamable stream)
        {
            agent.Enable(stream);
            return () =>
            {
                agent.Disable();            
            };
        }
    }
}
