namespace PinionCore.Remote.Client
{
    public class ConnectSet<TConnecter>
    {
        public ConnectSet(TConnecter listener, Remote.Ghost.IAgent agent)
        {
            Connector = listener;
            Agent = agent;
        }

        public readonly TConnecter Connector;
        public readonly Remote.Ghost.IAgent Agent;

    }
}
