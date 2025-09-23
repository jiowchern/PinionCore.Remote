using System;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway.Hosts
{
    internal class GatewayHostServiceHub
    {
        private readonly GatewayHostSessionCoordinator _sessionCoordinator;
        private readonly GatewayHostClientEntry _clientEntry;
        public readonly IService Service;
        public readonly IServiceRegistry Registry;

        public GatewayHostServiceHub(IGameLobbySelectionStrategy selectionStrategy = null)
        {
            _sessionCoordinator = new GatewayHostSessionCoordinator(selectionStrategy);
            Registry = _sessionCoordinator;
            _clientEntry = new GatewayHostClientEntry(_sessionCoordinator);
            var protocol = PinionCore.Remote.Gateway.Protocols.ProtocolProvider.Create();
            Service = PinionCore.Remote.Standalone.Provider.CreateService(_clientEntry, protocol);
        }
        
    }
}


