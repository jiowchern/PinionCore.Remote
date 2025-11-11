using System;
using System.Collections.Generic;

namespace PinionCore.Remote.Server
{


    internal sealed class MultiListener : PinionCore.Remote.Soul.IListenable
    {
        private readonly Depot<PinionCore.Remote.Soul.IListenable> _listenables;

        public MultiListener()
        {
            _listenables = new Depot<PinionCore.Remote.Soul.IListenable>();
        }

        public void Add(PinionCore.Remote.Soul.IListenable listenable)
        {
            _listenables.Items.Add(listenable);
        }

        public void Remove(PinionCore.Remote.Soul.IListenable listenable)
        {
            _listenables.Items.Remove(listenable);
        }

        event Action<PinionCore.Network.IStreamable> PinionCore.Remote.Soul.IListenable.StreamableEnterEvent
        {
            add
            {
                foreach (var listener in _listenables)
                {
                    listener.StreamableEnterEvent += value;
                }
            }
            remove
            {
                foreach (var listener in _listenables)
                {
                    listener.StreamableEnterEvent -= value;
                }
            }
        }

        event Action<PinionCore.Network.IStreamable> PinionCore.Remote.Soul.IListenable.StreamableLeaveEvent
        {
            add
            {
                foreach (var listener in _listenables)
                {
                    listener.StreamableLeaveEvent += value;
                }
            }
            remove
            {
                foreach (var listener in _listenables)
                {
                    listener.StreamableLeaveEvent -= value;
                }
            }
        }
    }
}
