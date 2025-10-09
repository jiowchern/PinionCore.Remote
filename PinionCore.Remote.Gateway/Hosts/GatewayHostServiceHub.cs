using System;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway.Hosts
{
    public class GatewayHostServiceHub
    {
        private readonly GatewayHostSessionCoordinator _sessionCoordinator;
        private readonly GatewayHostClientEntry _clientEntry;


        public readonly IService Source;
        public readonly IServiceRegistry Sink;

        public GatewayHostServiceHub(IGameLobbySelectionStrategy selectionStrategy )
        {
            _sessionCoordinator = new GatewayHostSessionCoordinator(selectionStrategy);
            Sink = _sessionCoordinator;
            _clientEntry = new GatewayHostClientEntry(_sessionCoordinator);
            var protocol = PinionCore.Remote.Gateway.Protocols.ProtocolProvider.Create();
            Source = PinionCore.Remote.Standalone.Provider.CreateService(_clientEntry, protocol);
        }
        
    }
}


