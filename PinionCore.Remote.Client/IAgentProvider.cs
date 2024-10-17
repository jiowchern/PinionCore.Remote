namespace PinionCore.Remote.Client
{
    public interface IAgentProvider
    {
        Ghost.IAgent Spawn();
    }
}