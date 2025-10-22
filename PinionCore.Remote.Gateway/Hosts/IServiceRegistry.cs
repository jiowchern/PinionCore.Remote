using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Hosts
{
    interface IServiceRegistry
    {
        void Register(Registrys.ILineAllocatable allocatable);
        void Unregister(Registrys.ILineAllocatable allocatable);
    }
}

