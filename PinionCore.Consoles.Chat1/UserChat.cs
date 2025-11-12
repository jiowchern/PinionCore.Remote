using System;
using System.Linq;
using PinionCore.Consoles.Chat1.Common;

namespace PinionCore.Consoles.Chat1
{
    internal sealed class UserChat : PinionCore.Utility.IBootable, IPlayer, IMessageable
    {
        private readonly PinionCore.Remote.ISessionBinder _binder;
        private readonly Room _room;
        private readonly string _name;
        private readonly PinionCore.Remote.Depot<IChatter> _chatters;

        private PinionCore.Remote.ISoul _selfSoul;
        private Chatter _chatter;

        private event Action<Message> _publicMessageEvent = delegate { };
        private event Action<Message> _privateMessageEvent = delegate { };

        public UserChat(PinionCore.Remote.ISessionBinder binder, Room room, string name)
        {
            _binder = binder;
            _room = room;
            _name = name;
            _chatters = new PinionCore.Remote.Depot<IChatter>();
        }

        public event Action DoneEvent = delegate { };

        string IMessageable.Name => _name;

        PinionCore.Remote.Notifier<IChatter> IPlayer.Chatters => new PinionCore.Remote.Notifier<IChatter>(_chatters);

        event Action<Message> IPlayer.PublicMessageEvent
        {
            add
            {
                _publicMessageEvent += value;
                foreach (var message in _room.Histroyable.Query())
                {
                    value(message);
                }
            }
            remove => _publicMessageEvent -= value;
        }

        event Action<Message> IPlayer.PrivateMessageEvent
        {
            add => _privateMessageEvent += value;
            remove => _privateMessageEvent -= value;
        }

        event Action<Message> IPlayer.AnnounceEvent
        {
            add => _room.AnnounceEvent += value;
            remove => _room.AnnounceEvent -= value;
        }

        void IPlayer.Quit()
        {
            DoneEvent();
        }

        void IPlayer.Send(string message)
        {
            _chatter.Send(message);
        }

        void PinionCore.Utility.IBootable.Launch()
        {
            _selfSoul = _binder.Bind<IPlayer>(this);
            _chatter = _room.RegistChatter(this);
            _chatters.Items.Clear();

            _room.Chatters.Supply += _AddChatter;
            _room.Chatters.Unsupply += _RemoveChatter;
        }

        private void _RemoveChatter(Chatter chatter)
        {
            var present = _chatters.Items.FirstOrDefault(i => i.Name == chatter.Messager.Name);
            if (present != null)
            {
                _chatters.Items.Remove(present);
            }
        }

        private void _AddChatter(Chatter chatter)
        {
            var whisperable = new WhispeableChatter(_chatter, chatter);
            _chatters.Items.Add(whisperable);
        }

        void PinionCore.Utility.IBootable.Shutdown()
        {
            if (_chatter != null)
            {
                _room.UnregistChatter(_chatter);
            }

            if (_selfSoul != null)
            {
                _binder.Unbind(_selfSoul);
            }

            _room.Chatters.Supply -= _AddChatter;
            _room.Chatters.Unsupply -= _RemoveChatter;
        }

        void IMessageable.PublicReceive(Message message)
        {
            _publicMessageEvent(message);
        }

        void IMessageable.PrivateReceive(Message message)
        {
            _privateMessageEvent(message);
        }
    }
}
