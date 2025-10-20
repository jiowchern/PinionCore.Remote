using System;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway.Hosts
{
    internal class ServiceHub
    {
        private readonly SessionCoordinator _sessionCoordinator;
        private readonly ClientEntry _clientEntry;


        public readonly IService Source;
        public readonly IServiceRegistry Sink;

        public ServiceHub(ISessionSelectionStrategy selectionStrategy)
        {
            _sessionCoordinator = new SessionCoordinator(selectionStrategy);
            Sink = _sessionCoordinator;
            _clientEntry = new ClientEntry(_sessionCoordinator);
            var protocol = PinionCore.Remote.Gateway.Protocols.ProtocolProvider.Create();
            Source = PinionCore.Remote.Standalone.Provider.CreateService(_clientEntry, protocol);
        }
        
    }
}


