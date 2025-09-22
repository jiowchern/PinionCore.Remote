using System;
using System.Net;
using System.Runtime.Serialization;
using PinionCore.Memorys;
using PinionCore.Network;
using PinionCore.Remote.Ghost;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway.Servers 
{
    class GatewayServerServiceHub 
    {
        readonly System.Action _dispose;
        
        public readonly IService Service;

        public readonly IListenable Listener;

        
        public GatewayServerServiceHub()
        {
            var clientListener = new PinionCore.Remote.Gateway.Servers.GatewayServerConnectionManager();
            var clientEntry = new GatewayServerClientEntry(clientListener);
            var clientProtocol = Protocols.ProtocolProvider.Create();
            Service = Standalone.Provider.CreateService(clientEntry, clientProtocol);
            
            Listener = clientListener;
            _dispose = () =>
            {
                Service.Dispose();                
            };
        }
   
        public void Dispose()
        {
            _dispose();
        }

        
    }
}

