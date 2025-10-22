using System;
using System.Collections.Generic;
using PinionCore.Network;
using PinionCore.Remote;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Hosts
{
    internal class ConnectionRoster : IRoutableSession, IConnectionRoster
    {
        private readonly object _syncRoot;
        private readonly Dictionary<uint, IStreamable> _streamsByGroup;
        private readonly Dictionary<IStreamable, int> _streamRefCounts;
        private readonly Depot<IStreamable> _streamCollection;
        private readonly Notifier<IStreamable> _streams;

        public ConnectionRoster()
        {
            _syncRoot = new object();
            _streamsByGroup = new Dictionary<uint, IStreamable>();
            _streamRefCounts = new Dictionary<IStreamable, int>();
            _streamCollection = new Depot<IStreamable>();
            _streams = new Notifier<IStreamable>(_streamCollection);
        }

        Notifier<IStreamable> IConnectionRoster.Connections => _streams;

        bool IRoutableSession.Set(uint group, IStreamable stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            lock (_syncRoot)
            {
                if (_streamsByGroup.ContainsKey(group))
                {
                    return false;
                }

                _streamsByGroup[group] = stream;
                if (_streamRefCounts.TryGetValue(stream, out var refCount))
                {
                    _streamRefCounts[stream] = refCount + 1;
                }
                else
                {
                    _streamRefCounts[stream] = 1;
                    _streamCollection.Items.Add(stream);
                }

                return true;
            }
        }

        bool IRoutableSession.Unset(uint group)
        {
            lock (_syncRoot)
            {
                if (!_streamsByGroup.TryGetValue(group, out var stream))
                {
                    return false;
                }

                _streamsByGroup.Remove(group);

                if (_streamRefCounts.TryGetValue(stream, out var refCount))
                {
                    refCount--;
                    if (refCount <= 0)
                    {
                        _streamRefCounts.Remove(stream);
                        _streamCollection.Items.Remove(stream);
                    }
                    else
                    {
                        _streamRefCounts[stream] = refCount;
                    }
                }

                return true;
            }
        }
    }
}

