using System;

namespace PinionCore.Remote.Gateway
{

    public interface IService
    {
        uint Id { get; }
        PinionCore.Network.IStreamable Streamable { get; }        
    }
}
