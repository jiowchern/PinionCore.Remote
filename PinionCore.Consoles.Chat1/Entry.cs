using System.Collections.Generic;

namespace PinionCore.Consoles.Chat1
{
    public class Entry : PinionCore.Remote.IEntry
    {
        private readonly Room _room;
        private readonly Dictionary<PinionCore.Remote.ISessionBinder, User> _users;
        private readonly object _sync = new object();
        private readonly PinionCore.Utility.Log _log;

        public Entry()
        {
            _room = new Room();
            _users = new Dictionary<PinionCore.Remote.ISessionBinder, User>();
            Announcement = _room;
            _log = PinionCore.Utility.Log.Instance;
        }

        public Announceable Announcement { get; }

        public void OnSessionOpened(PinionCore.Remote.ISessionBinder binder)
        {
            var user = new User(binder, _room);
            int currentCount;
            lock (_sync)
            {
                _users[binder] = user;
                currentCount = _users.Count;
            }
            _log.WriteInfo(() => $"[Entry] 客戶端連接建立 (當前連接數: {currentCount})");
        }

        public void OnSessionClosed(PinionCore.Remote.ISessionBinder binder)
        {
            User user = null;
            int currentCount;
            lock (_sync)
            {
                if (_users.TryGetValue(binder, out var existing))
                {
                    user = existing;
                    _users.Remove(binder);
                }
                currentCount = _users.Count;
            }

            user?.Dispose();
            _log.WriteInfo(() => $"[Entry] 客戶端連接中斷 (當前連接數: {currentCount})");
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

