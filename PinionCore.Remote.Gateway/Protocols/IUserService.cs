namespace PinionCore.Remote.Gateway.Protocols
{

    public interface IUserService
    {
        PinionCore.Remote.Value<uint> Join();
        PinionCore.Remote.Value<ReturnCode> Leave(uint user);
        PinionCore.Remote.Notifier<IServiceSession> UserNotifier { get; }
    }
}

