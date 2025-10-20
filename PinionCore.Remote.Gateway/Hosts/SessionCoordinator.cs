using System;
using System.Collections.Generic;
using System.Linq;
using PinionCore.Network;
using PinionCore.Remote.Gateway.Protocols;
using PinionCore.Remote.Gateway.Registrys;

namespace PinionCore.Remote.Gateway.Hosts
{
    internal sealed class SessionCoordinator : ISessionMembership, IServiceRegistry, IDisposable
    {
        private sealed class SessionState
        {
            public SessionState(IRoutableSession session)
            {
                Session = session ?? throw new ArgumentNullException(nameof(session));
            }

            public IRoutableSession Session { get; }
            public Dictionary<uint, Allocation> Allocations { get; } = new Dictionary<uint, Allocation>();
        }

        private readonly struct Allocation
        {
            public Allocation(ILineAllocatable allocator, IStreamable stream)
            {
                Allocator = allocator;
                Stream = stream;
            }

            public ILineAllocatable Allocator { get; }
            public IStreamable Stream { get; }
        }

        private readonly object _gate = new object();
        private readonly ISessionSelectionStrategy _strategy;
        private readonly Dictionary<uint, List<ILineAllocatable>> _allocatorsByGroup;
        private readonly Dictionary<IRoutableSession, SessionState> _sessions;
        private bool _disposed;

        public SessionCoordinator(ISessionSelectionStrategy strategy)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _allocatorsByGroup = new Dictionary<uint, List<ILineAllocatable>>();
            _sessions = new Dictionary<IRoutableSession, SessionState>();
        }

        public void Register(uint group, ILineAllocatable allocatable)
        {
            if (allocatable == null)
            {
                throw new ArgumentNullException(nameof(allocatable));
            }

            lock (_gate)
            {
                ThrowIfDisposed();
                var list = GetOrCreateAllocators(group);
                if (!list.Contains(allocatable))
                {
                    list.Add(allocatable);
                }

                foreach (var session in _sessions.Values)
                {
                    TryAssign(session, group);
                }
            }
        }

        public void Unregister(uint group, ILineAllocatable allocatable)
        {
            if (allocatable == null)
            {
                throw new ArgumentNullException(nameof(allocatable));
            }

            lock (_gate)
            {
                ThrowIfDisposed();
                if (!_allocatorsByGroup.TryGetValue(group, out var list))
                {
                    return;
                }

                list.Remove(allocatable);
                if (list.Count == 0)
                {
                    _allocatorsByGroup.Remove(group);
                }

                foreach (var session in _sessions.Values)
                {
                    if (!session.Allocations.TryGetValue(group, out var allocation))
                    {
                        continue;
                    }

                    if (!ReferenceEquals(allocation.Allocator, allocatable))
                    {
                        continue;
                    }

                    ReleaseAllocation(session, group, allocation);
                    TryAssign(session, group);
                }
            }
        }

        public void Join(IRoutableSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            lock (_gate)
            {
                ThrowIfDisposed();
                if (_sessions.ContainsKey(session))
                {
                    return;
                }

                var state = new SessionState(session);
                _sessions.Add(session, state);

                foreach (var group in _allocatorsByGroup.Keys.ToList())
                {
                    TryAssign(state, group);
                }
            }
        }

        public void Leave(IRoutableSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            lock (_gate)
            {
                if (!_sessions.Remove(session, out var state))
                {
                    return;
                }

                foreach (var pair in state.Allocations.ToArray())
                {
                    ReleaseAllocation(state, pair.Key, pair.Value);
                }
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                foreach (var state in _sessions.Values)
                {
                    foreach (var pair in state.Allocations.ToArray())
                    {
                        ReleaseAllocation(state, pair.Key, pair.Value);
                    }
                }

                _sessions.Clear();
                _allocatorsByGroup.Clear();
            }
        }

        private void TryAssign(SessionState state, uint group)
        {
            if (state.Allocations.ContainsKey(group))
            {
                return;
            }

            if (!_allocatorsByGroup.TryGetValue(group, out var list) || list.Count == 0)
            {
                return;
            }

            foreach (var allocator in _strategy.OrderLobbies(group, list))
            {
                IStreamable stream = null;
                try
                {
                    stream = allocator.Alloc();
                    if (stream == null)
                    {
                        continue;
                    }

                    if (!state.Session.Set(group, stream))
                    {
                        allocator.Free(stream);
                        continue;
                    }

                    state.Allocations[group] = new Allocation(allocator, stream);
                    return;
                }
                catch
                {
                    if (stream != null)
                    {
                        try
                        {
                            allocator.Free(stream);
                        }
                        catch
                        {
                            // ignore
                        }
                    }
                }
            }
        }

        private void ReleaseAllocation(SessionState state, uint group, Allocation allocation)
        {
            if (state.Session.Unset(group))
            {
                allocation.Allocator.Free(allocation.Stream);
            }

            state.Allocations.Remove(group);
        }

        private List<ILineAllocatable> GetOrCreateAllocators(uint group)
        {
            if (!_allocatorsByGroup.TryGetValue(group, out var list))
            {
                list = new List<ILineAllocatable>();
                _allocatorsByGroup.Add(group, list);
            }

            return list;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SessionCoordinator));
            }
        }
    }
}
