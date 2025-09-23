namespace PinionCore.Remote.Gateway.Hosts
{
    interface ISessionMembership
    {
        void Join(IRoutableSession session);
        void Leave(IRoutableSession session);
    }
}


