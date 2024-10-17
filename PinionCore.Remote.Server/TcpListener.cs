using PinionCore.Network;
using PinionCore.Network.Tcp;
using PinionCore.Remote.Soul;
using System;

namespace PinionCore.Remote.Server.Tcp
{
    public class Listener : Soul.IListenable
    {
        
        readonly PinionCore.Network.Tcp.Listener _Listener;
        readonly PinionCore.Remote.NotifiableCollection<IStreamable> _NotifiableCollection;
        public Listener()
        {

            _NotifiableCollection = new NotifiableCollection<IStreamable>();
            _Listener = new Network.Tcp.Listener();
            _Listener.AcceptEvent += _Join;

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

        public void Bind(int port)
        {
            _Listener.Bind(port);
        }

        public void Close() {
            _Listener.Close();
        }

        private void _Join(Peer peer)
        {
            peer.BreakEvent += () => {                 
                lock (_NotifiableCollection)
                    _NotifiableCollection.Items.Remove(peer);                
            };
            
            lock (_NotifiableCollection)
                _NotifiableCollection.Items.Add(peer);
        }

    }
}