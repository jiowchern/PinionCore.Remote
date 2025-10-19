using PinionCore.Network;

namespace PinionCore.Remote.Gateway.Protocols
{
    public interface ILoginable
    {
        PinionCore.Remote.Value Login(uint group);
    }

    public interface IRegisterable
    {
        Notifier<ILoginable> LoginNotifier { get; }
        Notifier<IStreamProviable> StreamsNotifier { get; }

    }

    [System.Obsolete]
    public interface IConnection : IStreamable
    {
        PinionCore.Remote.Property<uint> Id { get; }
    }
}

