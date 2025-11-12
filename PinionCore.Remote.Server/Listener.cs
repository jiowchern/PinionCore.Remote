using System;
using PinionCore.Network;
using PinionCore.Network.Tcp;

namespace PinionCore.Remote.Server.Tcp
{
    public class Listener : Remote.Soul.IListenable
    {

        PinionCore.Network.Tcp.Listener _Listener;
        readonly PinionCore.Remote.Depot<IStreamable> _ConnectedPeers;

        public event System.Action<int> DataReceivedEvent;
        public event System.Action<int> DataSentEvent;

        public Listener()
        {
            _ConnectedPeers = new Depot<IStreamable>();
            DataReceivedEvent += (size) => { };
            DataSentEvent += (size) => { };
        }

        event Action<IStreamable> Remote.Soul.IListenable.StreamableEnterEvent
        {
            add
            {
                _ConnectedPeers.Notifier.Supply += value;
            }

            remove
            {
                _ConnectedPeers.Notifier.Supply -= value;
            }
        }

        event Action<IStreamable> Remote.Soul.IListenable.StreamableLeaveEvent
        {
            add
            {
                _ConnectedPeers.Notifier.Unsupply += value;
            }

            remove
            {
                _ConnectedPeers.Notifier.Unsupply -= value;
            }
        }

        public void Bind(int port, int backlog = 10)
        {
            _Listener = new Network.Tcp.Listener();
            _Listener.AcceptEvent += _Join;
            _Listener.Bind(port, backlog);
        }

        public void Close()
        {
            _Listener.Close();
            _Listener.AcceptEvent -= _Join;
            _Listener = null;
            lock (_ConnectedPeers)
            {
                _ConnectedPeers.Items.Clear();
            }
        }

        private void _Join(Peer peer)
        {
            peer.BreakEvent += () =>
            {
                lock (_ConnectedPeers)
                {
                    peer.ReceiveEvent -= _Receive;
                    peer.SendEvent -= _Send;
                    _ConnectedPeers.Items.Remove(peer);
                }

            };

            lock (_ConnectedPeers)
            {
                peer.SendEvent += _Send;
                peer.ReceiveEvent += _Receive;
                _ConnectedPeers.Items.Add(peer);
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
