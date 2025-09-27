using System;
using System.Collections.Generic;

namespace PinionCore.Consoles.Chat1.Server
{
    internal sealed class CompositeListenable : PinionCore.Remote.Soul.IListenable
    {
        private readonly List<PinionCore.Remote.Soul.IListenable> _listenables;

        public CompositeListenable()
        {
            _listenables = new List<PinionCore.Remote.Soul.IListenable>();
        }

        public void Add(PinionCore.Remote.Soul.IListenable listenable)
        {
            _listenables.Add(listenable);
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
