using System;
using System.Linq;
using PinionCore.Remote.Gateway.Protocols;

using PinionCore.Remote.Ghost;

namespace PinionCore.Remote.Gateway.Hosts
{
    class GatewayHostClientAgentPool : IDisposable
    {
        class User
        {
            public IAgent Agent;
            public IClientConnection Session;            
        }

        readonly System.Collections.Generic.List<User> _users;
        
        readonly IProtocol _gameProtocol;
        System.Action _disable;
        public readonly Notifier<IAgent> Agents;
        public readonly IAgent Agent;
        public GatewayHostClientAgentPool(IProtocol gameProtocol) {
            
            _users = new System.Collections.Generic.List<User>();
            _gameProtocol = gameProtocol;
            Agent = PinionCore.Remote.Gateway.Provider.CreateAgent();
            Agents = new Notifier<IAgent>();

            Agent.QueryNotifier<IConnectionManager>().Supply += _Create;
            Agent.QueryNotifier<IConnectionManager>().Unsupply += _Destroy;

            _disable = () => {
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
            for (var i = _users.Count - 1; i >= 0; i--)
            {
                var user = _users[i];
                if (user.Session != session)
                {
                    continue;
                }
                
                user.Agent.Disable();
                
                _users.RemoveAt(i);
            }
        }

        private void _Create(IClientConnection session)
        {
            var agent = PinionCore.Remote.Standalone.Provider.CreateAgent(_gameProtocol);

            agent.Enable(new SessionAdapter(session));
            var user = new User
            {
                Agent = agent,
                Session = session,                
            };
            _users.Add(user);
            
        }
        public void Dispose()
        {
            // Dispose all tracked agents and unsubscribe from connection manager events
            for (var i = _users.Count - 1; i >= 0; i--)
            {
                var user = _users[i];
                
                user.Agent.Disable();
                
                _users.RemoveAt(i);
            }

            _disable();
        }
    }
}



