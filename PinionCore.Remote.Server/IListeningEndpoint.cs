using System;

namespace PinionCore.Remote.Server
{
    
    public interface IListeningEndpoint : Soul.IListenable , IDisposable
    {
        System.Threading.Tasks.Task<bool> ListenAsync();
    }


}
