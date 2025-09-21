using System;
using System.Linq;

using PinionCore.Remote.Gateway.Protocols;

using PinionCore.Remote.Ghost;

namespace PinionCore.Remote.Gateway.Hosts
{
    class GatewayAgent : IDisposable
    {
        class User
        {
            public IAgent Agent;
            public IServiceSession Session;
        }

        readonly System.Collections.Generic.List<User> _Users;
        
        readonly IProtocol _GameProtocol;
        System.Action _Disable;
        public readonly Notifier<IAgent> Agents;
        public readonly IAgent Agent;
        public GatewayAgent(IProtocol gameProtocol) {
            
            _Users = new System.Collections.Generic.List<User>();
            _GameProtocol = gameProtocol;
            Agent = PinionCore.Remote.Gateway.Provider.CreateAgent();
            Agents = new Notifier<IAgent>();

            Agent.QueryNotifier<IServiceSessionOwner>().Supply += _Create;
            Agent.QueryNotifier<IServiceSessionOwner>().Unsupply += _Destroy;

            _Disable = () => {
                Agent.QueryNotifier<IServiceSessionOwner>().Supply -= _Create;
                Agent.QueryNotifier<IServiceSessionOwner>().Unsupply -= _Destroy;
            };
        }

        private void _Destroy(IServiceSessionOwner owner)
        {
            owner.Sessions.Base.Supply -= _Create;
            owner.Sessions.Base.Unsupply -= _Destroy;
        }        

        private void _Create(IServiceSessionOwner owner)
        {
            owner.Sessions.Base.Supply += _Create;
            owner.Sessions.Base.Unsupply += _Destroy;
        }

        private void _Destroy(IServiceSession session)
        {
            var removes = from u in _Users where u.Session == session select u.Agent;
            foreach (var re in removes)
            {
                Agents.Collection.Remove(re);
                re.Disable();
            }
            _Users.RemoveAll(u => u.Session == session);              
        }

        private void _Create(IServiceSession session)
        {
            var agent = PinionCore.Remote.Standalone.Provider.CreateAgent(_GameProtocol);
            var stream = new Servers.UserStreamAdapter(session);
            agent.Enable(stream);
            _Users.Add(new User { Agent=agent, Session = session });
            Agents.Collection.Add(agent);
        }
        public void Dispose()
        {
            _Disable();
        }
    }
}
