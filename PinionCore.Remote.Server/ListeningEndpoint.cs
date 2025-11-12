using System;
using System.Threading.Tasks;
using PinionCore.Network;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Server
{
}

namespace PinionCore.Remote.Server.Tcp
{
    public class ListeningEndpoint : IListeningEndpoint
    {

        event Action<IStreamable> IListenable.StreamableEnterEvent
        {
            add
            {
                _Listenable.StreamableEnterEvent += value;
            }

            remove
            {
                _Listenable.StreamableEnterEvent -= value;
            }
        }

        event Action<IStreamable> IListenable.StreamableLeaveEvent
        {
            add
            {
                _Listenable.StreamableLeaveEvent += value;
            }

            remove
            {
                _Listenable.StreamableLeaveEvent -= value;
            }
        }

        public readonly int Port;
        public readonly int Backlog;
        readonly Listener _Listener;
        readonly Remote.Soul.IListenable _Listenable;
        public ListeningEndpoint(int port,int backlog)
        {
            Port = port;
            Backlog = backlog;
            _Listener = new Listener();
            _Listenable = _Listener;
        }

        Task<bool> IListeningEndpoint.ListenAsync()
        {
            _Listener.Bind(Port, Backlog);
            return Task.FromResult(true);
        }

        void IDisposable.Dispose()
        {
            _Listener.Close();
        }
    }
}
