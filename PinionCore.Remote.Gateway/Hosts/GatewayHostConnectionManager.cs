using System;
using System.Collections.Generic;
using PinionCore.Remote;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Hosts
{
    internal class GatewayHostConnectionManager : IRoutableSession, IConnectionManager
    {
        private readonly object _syncRoot;
        private readonly Dictionary<uint, IClientConnection> _sessionsByGroup;
        private readonly Dictionary<IClientConnection, int> _sessionRefCounts;
        private readonly Notifier<IClientConnection> _sessions;

        public GatewayHostConnectionManager()
        {
            _syncRoot = new object();
            _sessionsByGroup = new Dictionary<uint, IClientConnection>();
            _sessionRefCounts = new Dictionary<IClientConnection, int>();
            _sessions = new Notifier<IClientConnection>();
        }

        Notifier<IClientConnection> IConnectionManager.Connections => _sessions;

        bool IRoutableSession.Set(uint group, IClientConnection user)
        {
            // Validate arguments
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
                    // Notify when a session first gains a connection
                    _sessions.Collection.Add(user);
                }

                return true;
            }
        }

        bool IRoutableSession.Unset(uint group)
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


