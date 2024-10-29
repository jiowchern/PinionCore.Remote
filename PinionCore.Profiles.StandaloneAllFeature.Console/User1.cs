namespace PinionCore.Profiles.StandaloneAllFeature.Console
{
    class User
    {
        public readonly PinionCore.Remote.Ghost.IAgent Agent;
        public readonly int Id;
        public long Ticks;
        public User(PinionCore.Remote.Ghost.IAgent agent, int id)
        {
            Agent = agent;
            Id = id;
        }
    }
}
