using System;

namespace PinionCore.Remote.Server
{
    
    public interface IListeningEndpoint : Remote.Soul.IListenable , IDisposable
    {
        System.Threading.Tasks.Task<System.Exception> ListenAsync();
    }


}
