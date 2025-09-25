using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Hosts
{
    
    interface IServiceRegistry
    {
        void Register(uint group, ClientConnectionDisposer disposer);
        void Unregister(ClientConnectionDisposer disposer);
    }
}


