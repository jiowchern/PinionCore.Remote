using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Hosts
{
    interface IServiceRegistry
    {
        void Register(uint group, IGameLobby service);
        void Unregister(IGameLobby service);
    }
}


