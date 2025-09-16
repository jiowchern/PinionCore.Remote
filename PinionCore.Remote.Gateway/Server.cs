using System;
using PinionCore.Network;

namespace PinionCore.Remote.Gateway.Sessions
{
}
    
namespace PinionCore.Remote.Gateway
{

    class ServiceWorker : System.IDisposable
    {
        public ServiceWorker(IStreamable streamable, IService service) {

        }

        void IDisposable.Dispose()
        {
            throw new NotImplementedException();
        }
    }
    public class Server : System.IDisposable
    {
        readonly IStreamable _streamable;
        readonly System.Collections.Generic.List<ServiceWorker> _services = new System.Collections.Generic.List<ServiceWorker>();
        public Server(IStreamable streamable) {
            _streamable = streamable;
        }
        
        public void Register(IService service)
        {
            _services.Add(new ServiceWorker(_streamable,service));
            throw new System.NotImplementedException();
        }
        public void Unregister(IService service)
        {            
            throw new System.NotImplementedException();
        }

        void IDisposable.Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
