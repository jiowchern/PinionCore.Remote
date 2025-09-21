using System;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Server
{
    public class ListenSet<TListener, TService> : IDisposable where TListener : IListenable where TService : IService
    {
        public ListenSet(TListener listener, TService service)
        {
            Listener = listener;
            Service = service;

            Listener.StreamableEnterEvent += Service.Join;
            Listener.StreamableLeaveEvent += Service.Leave;
        }

        public readonly TListener Listener;
        public readonly TService Service;

        void IDisposable.Dispose()
        {
            Listener.StreamableEnterEvent -= Service.Join;
            Listener.StreamableLeaveEvent -= Service.Leave;
            Service.Dispose();
        }
    }
}
