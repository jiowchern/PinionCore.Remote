using PinionCore.Network;

namespace PinionCore.Remote.Gateway.Protocols
{
    
    public interface ILoginable
    {
        PinionCore.Remote.Value Login(uint group, byte[] version);
    }
}
