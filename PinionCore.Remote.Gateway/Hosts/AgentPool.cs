using System;
using System.ComponentModel;
using System.Linq;
using PinionCore.Remote.Gateway.Protocols;

using PinionCore.Remote.Ghost;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway.Hosts
{
    class AgentPool : IDisposable
    {
        class AgentSession : PinionCore.Network.IStreamable
        {
            public IAgent Agent;
            public IConnection Session;

            IAwaitableSource<int> PinionCore.Network.IStreamable.Send(byte[] buffer, int offset, int count)
            {
                // 直接轉發
                return Session.Send(buffer, offset, count);
            }

            IAwaitableSource<int> PinionCore.Network.IStreamable.Receive(byte[] buffer, int offset, int count)
            {
                // 直接轉發
                return Session.Receive(buffer, offset, count);
            }
        }


        readonly System.Collections.Generic.List<AgentSession> _Sessions;
        readonly IProtocol _gameProtocol;
        System.Action _disable;
        public readonly Notifier<IAgent> Agents;
        readonly NotifiableCollection<IAgent> _Agents;
        public readonly IAgent Agent;
        public AgentPool(IProtocol gameProtocol)
        {


            _gameProtocol = gameProtocol;
            _Sessions = new System.Collections.Generic.List<AgentSession>();
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

        private void _Destroy(IConnection session)
        {

            for (var i = _Sessions.Count - 1; i >= 0; i--)
            {
                var agentSession = _Sessions[i];
                if (agentSession.Session != session)
                {
                    continue;
                }

                agentSession.Agent.Disable();
                _Agents.Items.Remove(agentSession.Agent);
                _Sessions.RemoveAt(i);
            }
        }

        private void _Create(IConnection session)
        {
            var agent = PinionCore.Remote.Standalone.Provider.CreateAgent(_gameProtocol);

            var agentSession = new AgentSession
            {
                Agent = agent,
                Session = session,
            };
            agent.Enable(agentSession);
            _Sessions.Add(agentSession);
            _Agents.Items.Add(agent);

        }
        public void Dispose()
        {
            // Dispose all tracked agents and unsubscribe from connection manager events
            for (var i = _Sessions.Count - 1; i >= 0; i--)
            {
                var agentSession = _Sessions[i];

                agentSession.Agent.Disable();
                _Agents.Items.Remove(agentSession.Agent);
                _Sessions.RemoveAt(i);
            }

            _disable();
        }
    }
}



