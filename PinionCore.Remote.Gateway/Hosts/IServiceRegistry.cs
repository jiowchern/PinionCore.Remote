using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Hosts
{
    
    public interface IServiceRegistry
    {
        void Register(uint group, IConnectionProvider lobby);
        void Unregister(uint group, IConnectionProvider lobby);
    }
}


