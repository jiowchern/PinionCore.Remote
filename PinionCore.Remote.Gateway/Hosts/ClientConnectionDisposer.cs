using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PinionCore.Remote;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Hosts
{
    class ClientConnectionDisposer : IDisposable
    {
        

        public event Action<IClientConnection> ClientReleasedEvent;

        public ClientConnectionDisposer(IGameLobbySelectionStrategy selectionStrategy)
        {
        
        }

        public void Add(IGameLobby info)
        {
            throw new NotImplementedException();
        }

        public void Remove(IGameLobby lobby)
        {
            throw new NotImplementedException();
        }

        public Value<IClientConnection> Require()
        {
            throw new NotImplementedException();
        }

        public void Return(IClientConnection client)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

      
    }
}
