namespace PinionCore.Remote.Gateway.Tests
{
    public interface IGameUserAgent
    {
        void HandlePackets();
        void HandleMessage();
    }
}
