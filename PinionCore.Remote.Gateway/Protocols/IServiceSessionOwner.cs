namespace PinionCore.Remote.Gateway.Protocols
{
    public interface IServiceSessionOwner
    {
        PinionCore.Remote.Notifier<IServiceSession> Sessions { get; }
    }
}

