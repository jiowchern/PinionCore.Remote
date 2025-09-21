namespace PinionCore.Remote.Gateway.Hosts
{
    interface ISession
    {
        bool Set(uint group, PinionCore.Remote.Gateway.Protocols.IServiceSession user);
        bool Unset(uint group);
    }
}
