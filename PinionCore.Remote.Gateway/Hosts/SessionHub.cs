using System;
using System.Collections.Generic;

using PinionCore.Remote.Gateway.Registrys;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway.Hosts
{
   
    internal class SessionHub : ISessionMembershipProvider , IServiceRegistry , IDisposable
    {        
        private readonly Entry _Entry;
        
        readonly System.Collections.Generic.Dictionary<VersionKey, SessionCoordinator> _Coordinators;

        public readonly IService Source;
        public readonly IServiceRegistry Sink;
        readonly ISessionSelectionStrategy _SelectionStrategy;

        public SessionHub(ISessionSelectionStrategy selectionStrategy)
        {
            _Coordinators = new Dictionary<VersionKey, SessionCoordinator>();
            _SelectionStrategy = selectionStrategy;
            
            Sink = this;
            _Entry = new Entry(this);
            var protocol = PinionCore.Remote.Gateway.Protocols.ProtocolProvider.Create();
            
            Source = new PinionCore.Remote.Soul.Service(_Entry, protocol);
        }

        ISessionMembership ISessionMembershipProvider.Query(byte[] version)
        {
            return _Query(version);
        }

        private SessionCoordinator _Query(byte[] version)
        {
            lock(_Coordinators)
            {
                var key = new VersionKey(version);
                if (!_Coordinators.TryGetValue(key, out var coordinator))
                {
                    PinionCore.Utility.Log.Instance.WriteInfo($"Creating SessionCoordinator for version {key}");
                    coordinator = new SessionCoordinator(_SelectionStrategy);
                    _Coordinators[key] = coordinator;
                }
                return coordinator;
            }
        }

        void IServiceRegistry.Register(ILineAllocatable allocatable)
        {
            var coordinator = _Query(allocatable.Version);
            coordinator.Register(allocatable.Group, allocatable);

        }

        void IServiceRegistry.Unregister(ILineAllocatable allocatable)
        {
            var coordinator = _Query(allocatable.Version);
            coordinator.Unregister(allocatable.Group, allocatable);
        }

        public void Dispose()
        {
            lock (_Coordinators)
            {
                foreach (var coordinator in _Coordinators.Values)
                {
                    coordinator.Dispose();
                }
                _Coordinators.Clear();
            }
            _Entry.Dispose();
        }
    }
}


