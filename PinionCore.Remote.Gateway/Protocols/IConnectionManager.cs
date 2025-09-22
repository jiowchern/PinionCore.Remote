namespace PinionCore.Remote.Gateway.Protocols
{
    public interface IConnectionManager
    {
        PinionCore.Remote.Notifier<IClientConnection> Connections { get; }
    }
}

