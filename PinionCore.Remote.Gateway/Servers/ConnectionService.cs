using System;
using System.Net;
using System.Runtime.Serialization;
using PinionCore.Memorys;
using PinionCore.Network;
using PinionCore.Remote.Ghost;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway.Servers 
{
    class ConnectionService 
    {
        readonly System.Action _Dispose;
        
        public readonly IService Service;

        public readonly IListenable Listener;

        
        public ConnectionService()
        {
            var clientListener = new PinionCore.Remote.Gateway.Servers.ConnectionListener();
            var clientEntry = new ServiceEntryPoint(clientListener);
            var clientProtocol = Protocols.ProtocolProvider.Create();
            Service = Standalone.Provider.CreateService(clientEntry, clientProtocol);
            
            Listener = clientListener;
            _Dispose = () =>
            {
                Service.Dispose();                
            };
        }
   
        public void Dispose()
        {
            _Dispose();
        }

        
    }
}
