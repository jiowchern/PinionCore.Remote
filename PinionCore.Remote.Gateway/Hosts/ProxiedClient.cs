using System;
using System.Collections.Generic;
using PinionCore.Remote;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Hosts
{
    internal class ClientUser : ISession, IServiceSessionOwner
    {
        private readonly object _syncRoot;
        private readonly Dictionary<uint, IServiceSession> _sessionsByGroup;
        private readonly Dictionary<IServiceSession, int> _sessionRefCounts;
        private readonly Notifier<IServiceSession> _sessions;

        public ClientUser()
        {
            _syncRoot = new object();
            _sessionsByGroup = new Dictionary<uint, IServiceSession>();
            _sessionRefCounts = new Dictionary<IServiceSession, int>();
            _sessions = new Notifier<IServiceSession>();
        }

        Notifier<IServiceSession> IServiceSessionOwner.Sessions => _sessions;

        bool ISession.Set(uint group, IServiceSession user)
        {
            // 4. Argument validation 這邊沒進來
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            lock (_syncRoot)
            {
                if (_sessionsByGroup.ContainsKey(group))
                {
                    return false;
                }

                _sessionsByGroup[group] = user;

                if (_sessionRefCounts.TryGetValue(user, out var count))
                {
                    _sessionRefCounts[user] = count + 1;
                }
                else
                {
                    _sessionRefCounts[user] = 1;
                    // 5. Notify addition 這邊沒進來
                    _sessions.Collection.Add(user);
                }

                return true;
            }
        }

        bool ISession.Unset(uint group)
        {
            lock (_syncRoot)
            {
                if (!_sessionsByGroup.TryGetValue(group, out var user))
                {
                    return false;
                }

                _sessionsByGroup.Remove(group);

                if (_sessionRefCounts.TryGetValue(user, out var count))
                {
                    count--;
                    if (count <= 0)
                    {
                        _sessionRefCounts.Remove(user);
                        _sessions.Collection.Remove(user);
                    }
                    else
                    {
                        _sessionRefCounts[user] = count;
                    }
                }

                return true;
            }
        }
    }
}
