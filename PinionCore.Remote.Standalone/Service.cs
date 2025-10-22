


using System;
using System.Collections.Generic;
using System.IO;
using PinionCore.Memorys;
using PinionCore.Network;
using PinionCore.Remote.Ghost;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Standalone
{
    public class Service : Soul.IService
    {
        readonly PinionCore.Remote.Soul.IService _Service;
        readonly List<PinionCore.Remote.Ghost.IAgent> _Agents;
        readonly Dictionary<IAgent, IStreamable> _Streams;
        

        internal readonly IProtocol Protocol;
        internal readonly ISerializable Serializer;
        private readonly IPool _Pool;
        readonly Depot<IStreamable> _NotifiableCollection;
        public Service(IEntry entry, IProtocol protocol, ISerializable serializable, PinionCore.Remote.IInternalSerializable internal_serializable, Memorys.IPool pool)
        {
            _Pool = pool;
            _NotifiableCollection = new Depot<IStreamable>();
            Protocol = protocol;
            Serializer = serializable;
            
            var service = new PinionCore.Remote.Soul.AsyncService(new SyncService(entry, protocol, serializable, internal_serializable, _Pool));
            _Service = service;

            _Agents = new List<Ghost.IAgent>();
            _Streams = new Dictionary<IAgent, IStreamable>();
            
        }
        
       /* public Ghost.IAgent Create(IStreamable ghost,IStreamable soul)
        {            
            var agent = new PinionCore.Remote.Ghost.Agent(this.Protocol, this.Serializer, new PinionCore.Remote.InternalSerializer(), _Pool);
            agent.Enable(ghost);

            _NotifiableCollection.Items.Add(soul);
            _Streams.Add(agent, soul);
            _Agents.Add(agent);

            return agent;
        }
        public Ghost.IAgent Create()
        {
            return Create(new Stream());
        }
        public Ghost.IAgent Create(Stream stream)
        {
            var agentStream = new ReverseStream(stream);
            return Create(agentStream, stream);
        }


        public void Destroy(IAgent queryable)
        {
            var agents = new System.Collections.Generic.List<IAgent>();
            foreach (IAgent agent in _Agents)
            {
                if (agent != queryable)
                    continue;

                _NotifiableCollection.Items.Remove(_Streams[agent]);
                _Streams.Remove(agent);
                agent.Disable();
                agents.Add(agent);
            }
            foreach (IAgent agent in agents)
            {
                _Agents.Remove(agent);
            }

        }*/


        public void Dispose()
        {
            _Service.Dispose();
        }

        void IService.Join(IStreamable user)
        {
            _Service.Join(user);
            
        }

        void IService.Leave(IStreamable user)
        {
            _Service.Leave(user);
        }

        
    }
}
