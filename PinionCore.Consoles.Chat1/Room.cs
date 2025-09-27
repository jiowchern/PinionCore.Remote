using System;
using System.Collections.Generic;
using System.Linq;
using PinionCore.Consoles.Chat1.Common;
using PinionCore.Remote;

namespace PinionCore.Consoles.Chat1
{
    internal sealed class Room : IHistroyable, Announceable, IDisposable
    {
        private readonly ItemNotifier<Chatter> _chatters;
        private readonly List<Message> _messages;
        private readonly object _chattersLock;

        public Room()
        {
            _chatters = new ItemNotifier<Chatter>();
            _messages = new List<Message>();
            _chattersLock = new object();
            AnnounceEvent = _ => { };
            Histroyable = this;
        }

        public INotifier<Chatter> Chatters => _chatters;

        public IHistroyable Histroyable { get; }

        public event Action<Message> AnnounceEvent;

        internal Chatter RegistChatter(IMessageable messageable)
        {
            var chatter = new Chatter(messageable, message => Broadcast(message, messageable));
            lock (_chattersLock)
            {
                _chatters.Add(chatter);
            }

            return chatter;
        }

        internal void UnregistChatter(Chatter chatter)
        {
            lock (_chattersLock)
            {
                _chatters.Remove(chatter);
            }
        }

        private void Broadcast(string message, IMessageable sender)
        {
            var payload = new Message { Name = sender.Name, Context = message };

            lock (_messages)
            {
                if (_messages.Count >= 10)
                {
                    _messages.RemoveAt(0);
                }
                _messages.Add(payload);
            }

            List<Chatter> listeners;
            lock (_chattersLock)
            {
                listeners = _chatters.Items.ToList();
            }

            foreach (var chatter in listeners)
            {
                chatter.Messager.PublicReceive(payload);
            }
        }

        void Announceable.Announce(string name, string message)
        {
            AnnounceEvent(new Message { Name = name, Context = message });
        }

        public void Dispose()
        {
            lock (_chattersLock)
            {
                _chatters.Clear();
            }
        }

        Message[] IHistroyable.Query()
        {
            lock (_messages)
            {
                return _messages.ToArray();
            }
        }
    }
}
