using System;
using PinionCore.Network;
using PinionCore.Network.Web;

namespace PinionCore.Remote.Server.Web
{
    public class Listener : Remote.Soul.IListenable
    {

        readonly PinionCore.Network.Web.Listener _Listener;
        readonly PinionCore.Remote.Depot<IStreamable> _NotifiableCollection;
        public Listener()
        {

            _NotifiableCollection = new Depot<IStreamable>();
            _Listener = new Network.Web.Listener();

            _Listener.AcceptEvent += _Join;

        }

        event Action<IStreamable> Remote.Soul.IListenable.StreamableEnterEvent
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

        event Action<IStreamable> Remote.Soul.IListenable.StreamableLeaveEvent
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

        public Exception Bind(string address)
        {
            return _Listener.Bind(address);
        }

        public void Close()
        {
            _Listener.Close();
        }

        private void _Join(Peer peer)
        {
            peer.ErrorEvent += (status) =>
            {
                lock (_NotifiableCollection)
                    _NotifiableCollection.Items.Remove(peer);
            };
            lock (_NotifiableCollection)
                _NotifiableCollection.Items.Add(peer);
        }
    }
}
