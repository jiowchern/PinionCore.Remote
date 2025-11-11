
using System;
using System.Threading.Tasks;

namespace PinionCore.Remote.Client
{
    public interface IConnectingEndpoint : IDisposable
    {
        Task<Network.IStreamable> ConnectAsync();
    }
}
