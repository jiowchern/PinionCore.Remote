namespace PinionCore.Remote.Gateway.Hosts
{
    interface ISessionMembershipProvider
    {
        ISessionMembership Query(byte[] version);
    }
}


