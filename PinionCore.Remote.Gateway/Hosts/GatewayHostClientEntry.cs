using System;
using System.Net.Http.Headers;
using PinionCore.Network;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Hosts
{
    internal class GatewayHostClientEntry : IEntry
    {

        class User
        {
            public ISoul Soul;
            
            public GatewayHostConnectionRoster ConnectionManager;
        }

        private readonly ISessionMembership _sessionMembership;
        readonly System.Collections.Generic.Dictionary<IBinder, User> _Users;


        public GatewayHostClientEntry(ISessionMembership sessionMembership)
        {
            _sessionMembership = sessionMembership;
            _Users = new System.Collections.Generic.Dictionary<IBinder, User>();
        }
        void IBinderProvider.RegisterClientBinder(IBinder binder)
        {
            var user = new User();            
            if (!_Users.TryAdd(binder, user))
            {
                throw new ArgumentException("Binder already registered.", nameof(binder));
            }
            user.ConnectionManager = new GatewayHostConnectionRoster();
            // 3. Join the sessionMembership session
            _sessionMembership.Join(user.ConnectionManager);
            user.Soul = binder.Bind<IConnectionRoster>(user.ConnectionManager);
            
        }

        void IBinderProvider.UnregisterClientBinder(IBinder binder)
        {
            if(_Users.TryGetValue(binder, out User user))
            {
                _Users.Remove(binder);
                _sessionMembership.Leave(user.ConnectionManager);
                binder.Unbind(user.Soul);                
            }
            else
            {
                throw new ArgumentException("Binder not registered.", nameof(binder));
            }
        }

        void IEntry.Update()
        {
            
        }
    }
}


