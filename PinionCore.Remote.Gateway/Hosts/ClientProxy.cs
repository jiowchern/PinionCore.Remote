using System;
using System.Linq;

using PinionCore.Remote.Gateway.Protocols;

using PinionCore.Remote.Ghost;

namespace PinionCore.Remote.Gateway.Hosts
{
    class ClientProxy : IDisposable
    {
        class User
        {
            public IAgent Agent;
            public IClientConnection Session;
            public Servers.ClientStreamAdapter Stream;
        }

        readonly System.Collections.Generic.List<User> _Users;
        
        readonly IProtocol _GameProtocol;
        System.Action _Disable;
        public readonly Notifier<IAgent> Agents;
        public readonly IAgent Agent;
        public ClientProxy(IProtocol gameProtocol) {
            
            _Users = new System.Collections.Generic.List<User>();
            _GameProtocol = gameProtocol;
            Agent = PinionCore.Remote.Gateway.Provider.CreateAgent();
            Agents = new Notifier<IAgent>();

            Agent.QueryNotifier<IConnectionManager>().Supply += _Create;
            Agent.QueryNotifier<IConnectionManager>().Unsupply += _Destroy;

            _Disable = () => {
                Agent.QueryNotifier<IConnectionManager>().Supply -= _Create;
                Agent.QueryNotifier<IConnectionManager>().Unsupply -= _Destroy;
            };
        }

        private void _Destroy(IConnectionManager owner)
        {
            owner.Connections.Base.Supply -= _Create;
            owner.Connections.Base.Unsupply -= _Destroy;
        }        

        private void _Create(IConnectionManager owner)
        {
            owner.Connections.Base.Supply += _Create;
            owner.Connections.Base.Unsupply += _Destroy;
        }

        private void _Destroy(IClientConnection session)
        {
            for (var i = _Users.Count - 1; i >= 0; i--)
            {
                var user = _Users[i];
                if (user.Session != session)
                {
                    continue;
                }

                Agents.Collection.Remove(user.Agent);
                user.Agent.Disable();
                user.Stream?.Dispose();
                _Users.RemoveAt(i);
            }
        }

        private void _Create(IClientConnection session)
        {
            var agent = PinionCore.Remote.Standalone.Provider.CreateAgent(_GameProtocol);
            var stream = new Servers.ClientStreamAdapter(session);
            agent.Enable(stream);
            var user = new User
            {
                Agent = agent,
                Session = session,
                Stream = stream,
            };
            _Users.Add(user);
            Agents.Collection.Add(user.Agent);
        }
        public void Dispose()
        {
            foreach (var user in _Users)
            {
                Agents.Collection.Remove(user.Agent);
                user.Agent.Disable();
                user.Stream?.Dispose();
            }
            _Users.Clear();
            _Disable();
        }
    }
}
