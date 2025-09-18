


using System;
using System.Collections.Generic;
using PinionCore.Memorys;
using PinionCore.Network;
using PinionCore.Remote.Ghost;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Standalone
{
    public class Service : Soul.IService, Soul.IListenable
    {


        readonly PinionCore.Remote.Soul.IService _Service;
        readonly List<PinionCore.Remote.Ghost.IAgent> _Agents;
        readonly Dictionary<IAgent, IStreamable> _Streams;
        readonly IDisposable _ServiceDisposable;

        internal readonly IProtocol Protocol;
        internal readonly ISerializable Serializer;
        private readonly IPool _Pool;
        readonly NotifiableCollection<IStreamable> _NotifiableCollection;
        public Service(IEntry entry, IProtocol protocol, ISerializable serializable, PinionCore.Remote.IInternalSerializable internal_serializable, Memorys.IPool pool)
        {
            _Pool = pool;
            _NotifiableCollection = new NotifiableCollection<IStreamable>();
            Protocol = protocol;
            Serializer = serializable;
            var userProvider = new UserProvider(this, _Pool);
            var service = new PinionCore.Remote.Soul.AsyncService(new SyncService(entry, protocol, serializable, internal_serializable, _Pool, userProvider));
            _Service = service;

            _Agents = new List<Ghost.IAgent>();
            _Streams = new Dictionary<IAgent, IStreamable>();
            _ServiceDisposable = _Service;
        }


        event Action<IStreamable> Soul.IListenable.StreamableEnterEvent
        {
            add
            {
                _NotifiableCollection.Notifier.Supply += value;
            }

            remove
            {
                _NotifiableCollection.Notifier.Supply -= value;
            }
        }

        event Action<IStreamable> Soul.IListenable.StreamableLeaveEvent
        {
            add
            {
                _NotifiableCollection.Notifier.Unsupply += value;
            }

            remove
            {
                _NotifiableCollection.Notifier.Unsupply -= value;
            }
        }

        public Ghost.IAgent Create(Stream stream)
        {
            //var stream = new Stream();
            var agent = new PinionCore.Remote.Ghost.Agent(this.Protocol, this.Serializer, new PinionCore.Remote.InternalSerializer(), _Pool);
            agent.Enable(stream);
            var revStream = new ReverseStream(stream);
            _NotifiableCollection.Items.Add(revStream);
            _Streams.Add(agent, revStream);
            _Agents.Add(agent);

            return agent;
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

        }


        public void Dispose()
        {
            _ServiceDisposable.Dispose();
        }
    }
}
