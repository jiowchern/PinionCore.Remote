namespace PinionCore.Remote.Gateway.Protocols
{
    public interface IConnectionRoster
    {
        PinionCore.Remote.Notifier<IClientConnection> Connections { get; }
    }
}

