namespace PinionCore.Remote.Gateway.Protocols
{
    public interface IServiceSubUser
    {
        Notifier<IVerisable> Versions { get; }
        Notifier<IConnectionRoster> Rosters { get; }
    }
    public interface IVerisable
    {
        void Set(byte[] version);
    }
}

