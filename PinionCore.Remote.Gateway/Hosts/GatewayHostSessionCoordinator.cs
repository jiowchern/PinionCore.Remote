using System;
using System.Collections.Generic;
using System.Linq;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Hosts
{
    internal class GatewayHostSessionCoordinator : ISessionMembership, IServiceRegistry
    {
        public GatewayHostSessionCoordinator(IGameLobbySelectionStrategy strategy)
        {
            throw new NotImplementedException();
        }
        public void Join(IRoutableSession session)
        {
            throw new NotImplementedException();
        }

        public void Leave(IRoutableSession session)
        {
            throw new NotImplementedException();
        }

        public void Register(uint group, IGameLobby service)
        {
            throw new NotImplementedException();
        }

        public void Unregister(IGameLobby service)
        {
            throw new NotImplementedException();
        }
    }
   
}


