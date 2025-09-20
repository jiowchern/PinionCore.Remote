namespace PinionCore.Remote.Gateway.Protocols
{
    public interface IUser
    {
        PinionCore.Remote.Property<uint> Id { get; }
        event System.Action<byte[]> ResponseEvent;
        void Request(byte[] payload);
    }
}

