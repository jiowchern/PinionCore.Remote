using PinionCore.Network;

namespace PinionCore.Remote.Gateway.Hosts
{
    interface IRoutableSession
    {
        bool Set(uint group, IStreamable stream);
        bool Unset(uint group);
    }
}

