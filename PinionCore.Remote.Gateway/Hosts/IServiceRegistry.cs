using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Hosts
{
    interface IServiceRegistry
    {
        void Register(uint group, Registrys.ILineAllocatable allocatable);
        void Unregister(uint group, Registrys.ILineAllocatable allocatable);
    }
}

