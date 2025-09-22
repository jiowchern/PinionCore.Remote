namespace PinionCore.Remote.Gateway.Hosts
{
    interface IRoutableSession
    {
        bool Set(uint group, PinionCore.Remote.Gateway.Protocols.IClientConnection clientConnection);
        bool Unset(uint group);
    }
}
