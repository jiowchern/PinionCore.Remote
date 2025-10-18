using System;
using System.Net;
using System.Runtime.Serialization;
using PinionCore.Memorys;
using PinionCore.Network;
using PinionCore.Remote.Ghost;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway.Servers
{
    public class ServiceHub
    {
        readonly System.Action _dispose;

        // 接收來自 Gateway 的連線請求
        public readonly IService Source;


        // 發送來自 Gateway 的連線請求
        public readonly IListenable Sink;


        public ServiceHub()
        {
            var clientListener = new ConnectionPool();
            var clientEntry = new GatewayServerClientEntry(clientListener);
            var clientProtocol = Protocols.ProtocolProvider.Create();
            Source = Standalone.Provider.CreateService(clientEntry, clientProtocol);
            
            Sink = clientListener;
            _dispose = () =>
            {
                Source.Dispose();                
            };
        }
   
        public void Dispose()
        {
            _dispose();
        }

        
    }
}

