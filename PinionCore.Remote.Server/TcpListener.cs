using System;
using PinionCore.Network;
using PinionCore.Network.Tcp;

namespace PinionCore.Remote.Server.Tcp
{
    public class Listener : Soul.IListenable
    {

        PinionCore.Network.Tcp.Listener _Listener;
        readonly PinionCore.Remote.NotifiableCollection<IStreamable> _NotifiableCollection;

        public event System.Action<int> DataReceivedEvent;
        public event System.Action<int> DataSentEvent;

        public Listener()
        {

            _NotifiableCollection = new NotifiableCollection<IStreamable>();



            DataReceivedEvent += (size) => { };
            DataSentEvent += (size) => { };

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
            _Listener = new Network.Tcp.Listener();
            _Listener.AcceptEvent += _Join;
            _Listener.Bind(port, port);
        }

        public void Close()
        {
            _Listener.Close();
            _Listener.AcceptEvent -= _Join;
            _Listener = null;
            lock (_NotifiableCollection)
            {
                _NotifiableCollection.Items.Clear();
            }
        }

        private void _Join(Peer peer)
        {
            peer.BreakEvent += () =>
            {
                lock (_NotifiableCollection)
                {
                    peer.ReceiveEvent -= _Receive;
                    peer.SendEvent -= _Send;
                    _NotifiableCollection.Items.Remove(peer);
                }

            };

            lock (_NotifiableCollection)
            {
                peer.SendEvent += _Send;
                peer.ReceiveEvent += _Receive;
                _NotifiableCollection.Items.Add(peer);
            }

        }

        private void _Send(int bytes)
        {
            DataSentEvent(bytes);
        }

        private void _Receive(int bytes)
        {
            DataReceivedEvent(bytes);
        }
    }
}
