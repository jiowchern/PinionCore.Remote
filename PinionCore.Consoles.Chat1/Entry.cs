using System.Collections.Generic;

namespace PinionCore.Consoles.Chat1
{
    public class Entry : PinionCore.Remote.IEntry
    {
        private readonly Room _room;
        private readonly Dictionary<PinionCore.Remote.IBinder, User> _users;
        private readonly object _sync = new object();

        public Entry()
        {
            _room = new Room();
            _users = new Dictionary<PinionCore.Remote.IBinder, User>();
            Announcement = _room;
        }

        public Announceable Announcement { get; }

        public void RegisterClientBinder(PinionCore.Remote.IBinder binder)
        {
            var user = new User(binder, _room);
            lock (_sync)
            {
                _users[binder] = user;
            }
        }

        public void UnregisterClientBinder(PinionCore.Remote.IBinder binder)
        {
            User user = null;
            lock (_sync)
            {
                if (_users.TryGetValue(binder, out var existing))
                {
                    user = existing;
                    _users.Remove(binder);
                }
            }

            user?.Dispose();
        }

        public void Update()
        {
        }

        ~Entry()
        {
            lock (_sync)
            {
                foreach (var user in _users.Values)
                {
                    user.Dispose();
                }

                _users.Clear();
            }

            _room.Dispose();
        }
    }
}

