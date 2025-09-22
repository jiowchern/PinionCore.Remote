using System;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway.Hosts
{
    internal class GatewayCoordinator
    {
        private readonly SessionOrchestrator _SessionOrchestrator;
        private readonly ServiceEntryPoint _ServiceEntryPoint;
        public readonly IService Service;
        public readonly IServiceRegistry Registry;

        public GatewayCoordinator()
        {
            _SessionOrchestrator = new SessionOrchestrator();
            Registry = _SessionOrchestrator;
            _ServiceEntryPoint = new ServiceEntryPoint(_SessionOrchestrator);
            var protocol = PinionCore.Remote.Gateway.Protocols.ProtocolProvider.Create();
            Service = PinionCore.Remote.Standalone.Provider.CreateService(_ServiceEntryPoint, protocol);
        }
        
    }
}
