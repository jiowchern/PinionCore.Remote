using System;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway.Hosts
{
    internal class GatewayHost 
    {
        private readonly Router _Router;
        private readonly Entry _Entry;
        public readonly IService Service;
        public readonly IRouterServiceRegistry Registry;

        public GatewayHost()
        {
            _Router = new Router();
            Registry = _Router;
            _Entry = new Entry(_Router);
            var protocol = PinionCore.Remote.Gateway.Protocols.ProtocolProvider.Create();
            Service = PinionCore.Remote.Standalone.Provider.CreateService(_Entry, protocol);
        }
        
    }
}
