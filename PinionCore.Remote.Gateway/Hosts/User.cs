using System.Reflection;
using PinionCore.Remote.Gateway.Protocols;
using PinionCore.Utility;

namespace PinionCore.Remote.Gateway.Hosts
{
    class User : IServiceSubUser , PinionCore.Utility.IUpdatable
    {
        private readonly ISessionMembershipProvider _Provider;

        //public ISoul Soul;

        //public ConnectionRoster ConnectionManager;

        readonly PinionCore.Utility.StatusMachine _Machine;

        public User(ISessionMembershipProvider provider)
        {
            _Provider = provider;
            _Machine = new Utility.StatusMachine();
            _VersionsDepot = new SupplyDepot<IVerisable>();
            _RosterDepot = new SupplyDepot<IConnectionRoster>();
        }

        public User(ISessionBinder binder, ISessionMembershipProvider provider)
        {
            _Machine = new StatusMachine();
            Binder = binder;
            _Provider = provider;
            _VersionsDepot = new SupplyDepot<IVerisable>();
            _RosterDepot = new SupplyDepot<IConnectionRoster>();
        }

        readonly SupplyDepot<IVerisable> _VersionsDepot;
        Notifier<IVerisable> IServiceSubUser.Versions => _VersionsDepot.Supplier;

        readonly SupplyDepot<IConnectionRoster> _RosterDepot;
        public readonly ISessionBinder Binder;
        private ISoul _Soul;

        Notifier<IConnectionRoster> IServiceSubUser.Rosters => _RosterDepot.Supplier;

        void IBootable.Launch()
        {
            _Soul = Binder.Bind<IServiceSubUser>(this);
            _ToVersion();
        }

        private void _ToVersion()
        {
            var state = new UserVersionState(_VersionsDepot.Depot);
            state.DoneEvent += _ToRoster;
            _Machine.Push(state);
        }

        private void _ToRoster(byte[] version)
        {
            var state = new UserRosterState(version,_Provider,_RosterDepot.Depot);
            _Machine.Push(state);
        }

        void IBootable.Shutdown()
        {
            Binder.Unbind(_Soul);
            _Machine.Termination();
        }

        bool IUpdatable.Update()
        {
            _Machine.Update();
            return true;
        }
    }
}


