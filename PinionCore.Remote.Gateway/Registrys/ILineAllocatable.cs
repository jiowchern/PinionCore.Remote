using PinionCore.Network;

namespace PinionCore.Remote.Gateway.Registrys
{
    public interface ILineAllocatable
    {
        byte[] Version { get; }
        uint Group { get; }
        IStreamable Alloc();
        void Free(IStreamable stream);
    }
}

