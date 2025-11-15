using System;
using System.Threading.Tasks;
using PinionCore.Network;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Server.Web
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

        public readonly string Url;
        readonly Listener _Listener;
        readonly IListenable _Listenable;

        public ListeningEndpoint(string url)
        {
            Url = url;
            _Listener = new Listener();
            _Listenable = _Listener;
        }

        Task<System.Exception> IListeningEndpoint.ListenAsync()
        {                        
            return Task.FromResult<System.Exception>(_Listener.Bind(Url));
        }

        void IDisposable.Dispose()
        {
            _Listener.Close();
        }
    }
}
