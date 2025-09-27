namespace PinionCore.Remote.Gateway.Hosts
{
    interface IRoutableSession
    {
        bool Set(uint group, PinionCore.Remote.Gateway.Protocols.IConnection clientConnection);
        bool Unset(uint group);
    }
}
