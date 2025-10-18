using PinionCore.Network;

namespace PinionCore.Remote.Gateway.Protocols
{
    public interface IConnection : IStreamable
    {
        PinionCore.Remote.Property<uint> Id { get; }
    }
}

