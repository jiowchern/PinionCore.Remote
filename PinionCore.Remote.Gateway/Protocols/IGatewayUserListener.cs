namespace PinionCore.Remote.Gateway.Protocols
{

    public interface IGatewayUserListener
    {
        PinionCore.Remote.Value<uint> Join();
        PinionCore.Remote.Value<ReturnCode> Leave(uint user);
        PinionCore.Remote.Notifier<IUser> UserNotifier { get; }
    }
}

