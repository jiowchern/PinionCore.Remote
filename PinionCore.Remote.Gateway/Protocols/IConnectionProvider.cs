using PinionCore.Network;

namespace PinionCore.Remote.Gateway.Protocols
{
    [System.Obsolete]
    public interface IConnectionProvider
    {
        PinionCore.Remote.Value<uint> Join();
        PinionCore.Remote.Value<ResponseStatus> Leave(uint clientId);
        PinionCore.Remote.Notifier<IConnection> ConnectionNotifier { get; }
    }
    
    public interface IStreamProviable
    {
        void Exit();
        PinionCore.Remote.Notifier<IStreamable> Streams { get; }
    }
}

