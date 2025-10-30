using System;
using PinionCore.Network;
using PinionCore.Remote;
using PinionCore.Remote.Gateway.Protocols;
using PinionCore.Remote.Ghost;

namespace PinionCore.Remote.Gateway.Hosts
{
    public class AgentPool : IDisposable
    {
        private sealed class AgentSession : IStreamable
        {
            public IAgent Agent { get; set; }
            public IStreamable Stream { get; set; }

            public IAwaitableSource<int> Send(byte[] buffer, int offset, int count)
            {
                return Stream.Send(buffer, offset, count);
            }

            public IAwaitableSource<int> Receive(byte[] buffer, int offset, int count)
            {
                return Stream.Receive(buffer, offset, count);
            }

            void IDisposable.Dispose()
            {
                // AgentSession 不擁有 Stream 的所有權，由創建者負責釋放
            }
        }

        private readonly System.Collections.Generic.List<AgentSession> _sessions;
        private readonly IProtocol _Protocol;
        private readonly Depot<IAgent> _agentsCollection;
        private readonly Action _disable;

        public AgentPool(IProtocol gameProtocol)
        {
            _Protocol = gameProtocol;
            _sessions = new System.Collections.Generic.List<AgentSession>();
            Agent = Provider.CreateAgent();
            _agentsCollection = new Depot<IAgent>();
            Agents = new Notifier<IAgent>(_agentsCollection);

            Agent.QueryNotifier<IVerisable>().Supply += _OnVersionableSupply;
            Agent.QueryNotifier<IConnectionRoster>().Supply += OnRosterSupply;
            Agent.QueryNotifier<IConnectionRoster>().Unsupply += OnRosterUnsupply;

            _disable = () =>
            {
                Agent.QueryNotifier<IVerisable>().Supply -= _OnVersionableSupply;
                Agent.QueryNotifier<IConnectionRoster>().Supply -= OnRosterSupply;
                Agent.QueryNotifier<IConnectionRoster>().Unsupply -= OnRosterUnsupply;
            };
        }

        private void _OnVersionableSupply(IVerisable verisable)
        {
            verisable.Set(_Protocol.VersionCode);
        }

        public Notifier<IAgent> Agents { get; }
        public IAgent Agent { get; }

        private void OnRosterSupply(IConnectionRoster roster)
        {
            roster.Connections.Base.Supply += OnConnectionSupply;
            roster.Connections.Base.Unsupply += OnConnectionUnsupply;
        }

        private void OnRosterUnsupply(IConnectionRoster roster)
        {
            roster.Connections.Base.Supply -= OnConnectionSupply;
            roster.Connections.Base.Unsupply -= OnConnectionUnsupply;
        }

        private void OnConnectionSupply(IStreamable stream)
        {
            var agent = PinionCore.Remote.Standalone.Provider.CreateAgent(_Protocol);
            var wrapper = new AgentSession
            {
                Agent = agent,
                Stream = stream,
            };

            agent.Enable(wrapper);
            _sessions.Add(wrapper);
            _agentsCollection.Items.Add(agent);
        }

        private void OnConnectionUnsupply(IStreamable stream)
        {
            for (var i = _sessions.Count - 1; i >= 0; i--)
            {
                var session = _sessions[i];
                if (!ReferenceEquals(session.Stream, stream))
                {
                    continue;
                }

                session.Agent.Disable();
                _agentsCollection.Items.Remove(session.Agent);
                _sessions.RemoveAt(i);
            }
        }

        public void Dispose()
        {
            for (var i = _sessions.Count - 1; i >= 0; i--)
            {
                var session = _sessions[i];
                session.Agent.Disable();
                _agentsCollection.Items.Remove(session.Agent);
            }

            _sessions.Clear();
            _disable();
        }
    }
}



