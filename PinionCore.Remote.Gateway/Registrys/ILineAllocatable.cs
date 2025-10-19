using PinionCore.Network;

namespace PinionCore.Remote.Gateway.Registrys
{
    public interface ILineAllocatable
    {
        uint Group { get; }
        IStreamable Alloc();
        void Free(IStreamable stream);
    }
}

