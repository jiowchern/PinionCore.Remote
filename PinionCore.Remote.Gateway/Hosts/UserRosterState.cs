using System.Collections.Generic;
using PinionCore.Remote.Gateway.Protocols;
using PinionCore.Utility;

namespace PinionCore.Remote.Gateway.Hosts
{
    class UserRosterState : PinionCore.Utility.IStatus
    {
        private readonly byte[] _Version;
        private readonly ISessionMembershipProvider _Provider;
        private readonly ICollection<IConnectionRoster> _Rosters;
        readonly ConnectionRoster _Roster;
        public UserRosterState(byte[] version,ISessionMembershipProvider provider, ICollection<IConnectionRoster> rosters)
        {
            _Roster = new ConnectionRoster();
            _Version = version;
            _Provider = provider;
            _Rosters = rosters;
        }

        void IStatus.Enter()
        {
            var membership = _Provider.Query(_Version);
            membership.Join(_Roster);
            _Rosters.Add(_Roster);
        }

        void IStatus.Leave()
        {
            var membership = _Provider.Query(_Version);
            membership.Leave(_Roster);
            _Rosters.Remove(_Roster);
        }

        void IStatus.Update()
        {
            
        }
    }
}


