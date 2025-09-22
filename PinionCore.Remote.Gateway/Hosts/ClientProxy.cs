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
            public Servers.ClientStreamAdapter StreamAdapter;
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
            var removes = from u in _Users where u.Session == session select u;
            foreach (var user in removes)
            {
                user.StreamAdapter?.Dispose();
                user.StreamAdapter = null;

                Agents.Collection.Remove(user.Agent);
                user.Agent.Disable();
            }
            _Users.RemoveAll(u => u.Session == session);
        }

        private void _Create(IClientConnection session)
        {
            var agent = PinionCore.Remote.Standalone.Provider.CreateAgent(_GameProtocol);
            var stream = new Servers.ClientStreamAdapter(session);
            agent.Enable(stream);
            _Users.Add(new User { Agent=agent, Session = session, StreamAdapter = stream });
            Agents.Collection.Add(agent);
        }
        public void Dispose()
        {
            _Disable();

            foreach (var user in _Users)
            {
                Agents.Collection.Remove(user.Agent);
                user.Agent.Disable();
                user.StreamAdapter?.Dispose();
                user.StreamAdapter = null;
            }

            _Users.Clear();
        }
    }
}
