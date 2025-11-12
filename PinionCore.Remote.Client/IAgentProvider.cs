namespace PinionCore.Remote.Client
{
    public interface IAgentProvider
    {
        Remote.Ghost.IAgent Spawn();
    }
}
