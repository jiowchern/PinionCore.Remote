namespace PinionCore.Remote.Gateway.Protocols
{

    public interface IGameLobby
    {
        PinionCore.Remote.Value<uint> Join();
        PinionCore.Remote.Value<ResponseStatus> Leave(uint clientId);
        PinionCore.Remote.Notifier<IClientConnection> ClientNotifier { get; }
    }
}

