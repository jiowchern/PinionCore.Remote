using System;
using PinionCore.Network;
using PinionCore.Remote.Gateway.Protocols;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway.Servers 
{
    class Listener : IGameService , IListenable 
    {
        readonly IdProvider _IdProvider;
        readonly System.Collections.Concurrent.ConcurrentDictionary<uint, IServiceSession> _Users = new System.Collections.Concurrent.ConcurrentDictionary<uint, IServiceSession>();
        readonly Notifier<IServiceSession> _UserNotifier;
        

        public Listener()
        {
            _IdProvider = new IdProvider();
            _Users = new System.Collections.Concurrent.ConcurrentDictionary<uint, IServiceSession>();
            _UserNotifier = new Notifier<IServiceSession>();
        }

        
        Notifier<IServiceSession> IGameService.UserNotifier => _UserNotifier;

        event Action<IStreamable> _StreamableEnterEvent;
        event Action<IStreamable> IListenable.StreamableEnterEvent
        {
            add
            {
                _StreamableEnterEvent += value;
            }

            remove
            {
                _StreamableEnterEvent -= value;
            }
        }

        event Action<IStreamable> _StreamableLeaveEvent;
        event Action<IStreamable> IListenable.StreamableLeaveEvent
        {
            add
            {
                _StreamableLeaveEvent += value;
            }

            remove
            {
                _StreamableLeaveEvent -= value;
            }
        }

        Value<uint> IGameService.Join()
        {
            var id = _IdProvider.Landlord.Rent();
            var user = new User(id);
            if(!_Users.TryAdd(id, user))
            {
                throw new InvalidOperationException("Failed to add new user.");
            }
            _UserNotifier.Collection.Add(user);
            UserStreamRegistry.Register(id, user);
            _StreamableEnterEvent?.Invoke(user);
            return id;
        }

        Value<ReturnCode> IGameService.Leave(uint user)
        {
            var code = ReturnCode.NotFound;
            if (_Users.TryRemove(user, out var u))
            {
                _UserNotifier.Collection.Remove(u);
                UserStreamRegistry.Unregister(user);
                if (u is IStreamable streamable)
                {
                    _StreamableLeaveEvent?.Invoke(streamable);
                }
                code = ReturnCode.Success;
            }
            return code;
        }
    }
}
