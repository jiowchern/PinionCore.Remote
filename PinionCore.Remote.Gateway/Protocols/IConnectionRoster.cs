using PinionCore.Network;

namespace PinionCore.Remote.Gateway.Protocols
{
    public interface IConnectionRoster
    {
        PinionCore.Remote.Notifier<IStreamable> Connections { get; }
    }
}

