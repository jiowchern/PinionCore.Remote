using System;
using System.Collections.Generic;
using PinionCore.Network;
using PinionCore.Remote.Gateway.Hosts;
using PinionCore.Remote.Ghost;


namespace PinionCore.Remote.Gateway
{
    public class Agent : IAgent
    {
        readonly Dictionary<Type, INotifierOwneable> _NotifierOwners;
        readonly List<IAgent> _Agents;
        readonly AgentPool _Pool;

        public Agent(AgentPool pool)
        {
            _Agents = new List<IAgent>();
            _NotifierOwners = new Dictionary<Type, INotifierOwneable>();
            _Pool = pool;
        }
        float IAgent.Ping => _AvgPing();

        private float _AvgPing()
        {
            var total = _Pool.Agent.Ping;
            foreach(var agent in _Agents)
            {
                total += agent.Ping;
            }
            
            return total / (_Agents.Count + 1);
        }

        event Action<byte[], byte[]> _VersionCodeErrorEvent;
        event Action<byte[], byte[]> IAgent.VersionCodeErrorEvent
        {
            add
            {
                _VersionCodeErrorEvent += value;
            }

            remove
            {
                _VersionCodeErrorEvent -= value;
            }
        }

        event Action<string, string> _ErrorMethodEvent;
        event Action<string, string> IAgent.ErrorMethodEvent
        {
            add
            {
                _ErrorMethodEvent += value  ;
            }

            remove
            {
                _ErrorMethodEvent -= value;
            }
        }

        event Action<Exception> _ExceptionEvent;
        event Action<Exception> IAgent.ExceptionEvent
        {
            add
            {
                _ExceptionEvent += value;
            }

            remove
            {
                _ExceptionEvent -= value;
            }
        }

        void IAgent.Disable()
        {
            _Pool.Agent.Disable();
            _TearDownAgent(_Pool.Agent);
            _Pool.Agents.Base.Supply -= _SetupSubAgent;
            _Pool.Agents.Base.Unsupply -= _TearDownSubAgent;
        }

        void IAgent.Enable(IStreamable streamable)
        {
            _Pool.Agents.Base.Unsupply += _TearDownSubAgent;
            _Pool.Agents.Base.Supply += _SetupSubAgent;
            _SetupAgent(_Pool.Agent);            
            _Pool.Agent.Enable(streamable);
        }

        private void _SetupSubAgent(IAgent agent)
        {
            _SetupAgent(agent);
            _Agents.Add(agent);
            foreach (var notifierOwner in _NotifierOwners.Values)
            {
                notifierOwner.AddNotifier(agent);
            }
        }

        private void _TearDownSubAgent(IAgent agent)
        {
            _TearDownAgent(agent);
            _Agents.Remove(agent);
            foreach (var notifierOwner in _NotifierOwners.Values)
            {
                notifierOwner.RemoveNotifier(agent);
            }
        }

        private void _SetupAgent(IAgent agent)
        {
            agent.VersionCodeErrorEvent += _VersionCodeErrorEvent;
            agent.ExceptionEvent += _ExceptionEvent;
            agent.ErrorMethodEvent += _ErrorMethodEvent;
         
        }

        private void _TearDownAgent(IAgent agent)
        {
            
            agent.VersionCodeErrorEvent -= _VersionCodeErrorEvent;
            agent.ExceptionEvent -= _ExceptionEvent;
            agent.ErrorMethodEvent -= _ErrorMethodEvent;


        }

        void IAgent.HandleMessage()
        {

            _Pool.Agent.HandleMessage();

            foreach(var agent in _Agents)
            {
                agent.HandleMessage();
            }
        }

        void IAgent.HandlePackets()
        {
            _Pool.Agent.HandlePackets();

            foreach(var agent in _Agents)
            {
                agent.HandlePackets();
            }
        }

        INotifier<T> INotifierQueryable.QueryNotifier<T>()
        {
            if (_NotifierOwners.TryGetValue(typeof(T), out var owner))
            {
                return owner as INotifier<T>;
            }

            var notifier = new CompositeNotifier<T>();
            _NotifierOwners.Add(typeof(T), notifier);
            foreach(var agent in _Agents)
            {
                notifier.AddNotifier(agent);
            }
            return notifier;
        }
    }
}



