namespace PinionCore.Remote.Client
{
    public class Proxy 
    {
        public readonly Remote.Ghost.IAgent Agent;
        public Proxy(IProtocol protocol)            
        {
            Agent = new Remote.Ghost.Agent(protocol);
        }
    }
}
