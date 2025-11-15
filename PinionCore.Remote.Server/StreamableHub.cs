using System;
using System.Collections.Generic;
using PinionCore.Network;

namespace PinionCore.Remote.Server
{


    internal sealed class StreamableHub : PinionCore.Remote.Soul.IListenable 
    {
        private readonly Depot<IStreamable> _Listenables;

        public StreamableHub()
        {
            _Listenables = new Depot<IStreamable>();
        }

        public void Add(IStreamable streamable)
        {
            _Listenables.Items.Add(streamable);
        }

        public void Remove(IStreamable streamable)
        {
            _Listenables.Items.Remove(streamable);
        }

        event Action<PinionCore.Network.IStreamable> PinionCore.Remote.Soul.IListenable.StreamableEnterEvent
        {
            add
            {
                _Listenables.Notifier.Supply += value;
                
            }
            remove
            {
                _Listenables.Notifier.Supply -= value;
            }
        }

        event Action<PinionCore.Network.IStreamable> PinionCore.Remote.Soul.IListenable.StreamableLeaveEvent
        {
            add
            {
                _Listenables.Notifier.Unsupply += value;
            }
            remove
            {
                _Listenables.Notifier.Unsupply -= value;
            }
        }
    }
}
