using System;
using System.Collections.Generic;
using System.Linq;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Hosts
{


    internal class GatewayHostSessionCoordinator : ISessionMembership, IServiceRegistry , IDisposable
    {
        private readonly IGameLobbySelectionStrategy _SelectionStrategy;
               
        public GatewayHostSessionCoordinator(IGameLobbySelectionStrategy selectionStrategy)
        {
            _SelectionStrategy = selectionStrategy;
        }

        void ISessionMembership.Join(IRoutableSession session)
        {
            throw new NotImplementedException();
        }

        void ISessionMembership.Leave(IRoutableSession session)
        {
            throw new NotImplementedException();
        }

        void IServiceRegistry.Register(uint group, ClientConnectionDisposer service)
        {
            throw new NotImplementedException();
        }

        void IServiceRegistry.Unregister(ClientConnectionDisposer service)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {            
        }
    }
}


