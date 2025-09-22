namespace PinionCore.Remote.Gateway.Protocols
{
    public interface IClientConnection
    {
        PinionCore.Remote.Property<uint> Id { get; }
        event System.Action<byte[]> ResponseEvent;
        void Request(byte[] payload);
    }
}

