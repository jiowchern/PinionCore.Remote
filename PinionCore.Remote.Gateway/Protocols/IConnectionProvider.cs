namespace PinionCore.Remote.Gateway.Protocols
{

    public interface IConnectionProvider
    {
        PinionCore.Remote.Value<uint> Join();
        PinionCore.Remote.Value<ResponseStatus> Leave(uint clientId);
        PinionCore.Remote.Notifier<IConnection> ConnectionNotifier { get; }
    }
}

