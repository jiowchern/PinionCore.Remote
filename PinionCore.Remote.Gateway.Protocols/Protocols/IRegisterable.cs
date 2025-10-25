namespace PinionCore.Remote.Gateway.Protocols
{
    public interface IRegisterable
    {
        Notifier<ILoginable> LoginNotifier { get; } 
        Notifier<IStreamProviable> StreamsNotifier { get; }

    }
}
