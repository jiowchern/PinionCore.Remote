using System;
using System.Collections.Generic;
using PinionCore.Remote;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Hosts
{
    internal class GatewayHostConnectionRoster : IRoutableSession, IConnectionRoster
    {
        private readonly object _syncRoot;
        private readonly Dictionary<uint, IClientConnection> _sessionsByGroup;
        private readonly Dictionary<IClientConnection, int> _sessionRefCounts;
        private readonly Notifier<IClientConnection> _sessions;
        private readonly NotifiableCollection<IClientConnection> _sessionColl;

        public GatewayHostConnectionRoster()
        {
            _syncRoot = new object();
            _sessionsByGroup = new Dictionary<uint, IClientConnection>();
            _sessionRefCounts = new Dictionary<IClientConnection, int>();
            _sessionColl = new NotifiableCollection<IClientConnection>();
            _sessions = new Notifier<IClientConnection>(_sessionColl);
        }

        Notifier<IClientConnection> IConnectionRoster.Connections => _sessions;

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
                    _sessionColl.Items.Add(user);
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
                        _sessionColl.Items.Remove(user);
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


