namespace PinionCore.Remote.Gateway.Protocols
{

    public interface IConnectionLobby
    {
        PinionCore.Remote.Value<uint> Join();
        PinionCore.Remote.Value<ResponseStatus> Leave(uint clientId);
        PinionCore.Remote.Notifier<IClientConnection> ClientNotifier { get; }
    }
}

