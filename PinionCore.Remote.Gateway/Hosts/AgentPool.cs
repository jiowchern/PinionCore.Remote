using System;
using System.Threading;
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

            // 通道已死(對端 stream soul 解綁)時通知 pool 拆除此 session
            public Action DeadEvent { get; set; }

            public IAwaitableSource<int> Send(byte[] buffer, int offset, int count, CancellationToken token)
            {
                if (token.IsCancellationRequested)
                    return 0.ToWaitableValue();
                return Stream.Send(buffer, offset, count, token);
            }

            public IAwaitableSource<int> Receive(byte[] buffer, int offset, int count, CancellationToken token)
            {
                if (token.IsCancellationRequested)
                    return 0.ToWaitableValue();

                IAwaitableSource<int> inner = Stream.Receive(buffer, offset, count, token);
                IAwaitable<int> awaiter = inner.GetAwaiter();
                var result = new Value<int>();
                void Complete()
                {
                    var readCount = awaiter.GetResult();
                    result.SetValue(readCount);
                    // 對端 stream soul 已解綁時,tunnel RPC 以錯誤完成並回 0:
                    // 視為連線已死,立即拆除 session,而不是在死通道上無聲空轉
                    if (readCount <= 0)
                        DeadEvent?.Invoke();
                }
                if (awaiter.IsCompleted)
                {
                    Complete();
                }
                else
                {
                    awaiter.OnCompleted(Complete);
                }
                return result;
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
            Agent = ProtocolProvider.Create().ToAgent();
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
            var agent = _Protocol.ToAgent();
            var wrapper = new AgentSession
            {
                Agent = agent,
                Stream = stream,
            };
            wrapper.DeadEvent = () => _OnSessionDead(wrapper);

            PinionCore.Utility.Log.Instance.WriteInfo($"AgentPool connection supply stream:{stream.GetHashCode()}");
            agent.Enable(wrapper);
            _sessions.Add(wrapper);
            _agentsCollection.Items.Add(agent);
        }

        private void _OnSessionDead(AgentSession session)
        {
            if (!_sessions.Remove(session))
            {
                return;
            }

            PinionCore.Utility.Log.Instance.WriteInfo($"AgentPool connection dead stream:{session.Stream.GetHashCode()}");
            session.Agent.Disable();
            _agentsCollection.Items.Remove(session.Agent);
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

                PinionCore.Utility.Log.Instance.WriteInfo($"AgentPool connection unsupply stream:{stream.GetHashCode()}");
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



