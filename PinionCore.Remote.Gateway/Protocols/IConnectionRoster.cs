namespace PinionCore.Remote.Gateway.Protocols
{
    public interface IConnectionRoster
    {
        PinionCore.Remote.Notifier<IConnection> Connections { get; }
    }
}

