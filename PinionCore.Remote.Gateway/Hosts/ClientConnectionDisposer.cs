using System;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Hosts
{
    class ClientConnectionDisposer : IDisposable
    {

        private readonly IGameLobbySelectionStrategy _SelectionStrategy;

        public ClientConnectionDisposer(IGameLobbySelectionStrategy selectionStrategy)
        {
            _SelectionStrategy = selectionStrategy;
        }
        public void Add(IGameLobby info)
        {
            throw new NotImplementedException();
        }

        public void Remove(IGameLobby lobby)
        {
            throw new NotImplementedException();

        }

        void IDisposable.Dispose()
        {

        }


        
        public PinionCore.Remote.Value<IClientConnection> Require()
        {
            
            throw new NotImplementedException();
        }


        public void Return(IClientConnection client)
        {           

            throw new NotImplementedException();
        }
    }
}


