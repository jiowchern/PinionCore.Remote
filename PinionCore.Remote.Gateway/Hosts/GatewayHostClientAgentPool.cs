using System;
using System.ComponentModel;
using System.Linq;
using PinionCore.Remote.Gateway.Protocols;

using PinionCore.Remote.Ghost;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway.Hosts
{
    class GatewayHostClientAgentPool : IDisposable
    {
        class User
        {
            public IAgent Agent;
            public IClientConnection Session;            
        }


        readonly System.Collections.Generic.List<User> _Users;
        readonly IProtocol _gameProtocol;
        System.Action _disable;
        public readonly Notifier<IAgent> Agents;
        readonly NotifiableCollection<IAgent> _Agents;
        public readonly IAgent Agent;
        public GatewayHostClientAgentPool(IProtocol gameProtocol) {
            
            
            _gameProtocol = gameProtocol;
            _Users = new System.Collections.Generic.List<User>();
            Agent = Provider.CreateAgent();
            _Agents = new NotifiableCollection<IAgent>();
            Agents = new Notifier<IAgent>(_Agents);

            Agent.QueryNotifier<IConnectionRoster>().Supply += _Create;
            Agent.QueryNotifier<IConnectionRoster>().Unsupply += _Destroy;

            _disable = () => {
                Agent.QueryNotifier<IConnectionRoster>().Supply -= _Create;
                Agent.QueryNotifier<IConnectionRoster>().Unsupply -= _Destroy;
            };
        }

        private void _Destroy(IConnectionRoster owner)
        {
            owner.Connections.Base.Supply -= _Create;
            owner.Connections.Base.Unsupply -= _Destroy;
        }        

        private void _Create(IConnectionRoster owner)
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
                
                user.Agent.Disable();
                _Agents.Items.Remove(user.Agent);
                _Users.RemoveAt(i);
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
            _Users.Add(user);
            _Agents.Items.Add(agent);

        }
        public void Dispose()
        {
            // Dispose all tracked agents and unsubscribe from connection manager events
            for (var i = _Users.Count - 1; i >= 0; i--)
            {
                var user = _Users[i];
                
                user.Agent.Disable();
                _Agents.Items.Remove(user.Agent);
                _Users.RemoveAt(i);
            }

            _disable();
        }
    }
}



