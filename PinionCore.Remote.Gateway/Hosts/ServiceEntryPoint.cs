using System;
using System.Net.Http.Headers;
using PinionCore.Network;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Hosts
{
    internal class ServiceEntryPoint : IEntry
    {

        class User
        {
            public ISoul Soul;
            
            public ProxiedClient ProxiedClient;
        }

        private readonly ISessionMembership _Router;
        readonly System.Collections.Generic.Dictionary<IBinder, User> _Users;


        public ServiceEntryPoint(ISessionMembership router)
        {
            _Router = router;
            _Users = new System.Collections.Generic.Dictionary<IBinder, User>();
        }
        void IBinderProvider.RegisterClientBinder(IBinder binder)
        {
            var user = new User();            
            if (!_Users.TryAdd(binder, user))
            {
                throw new ArgumentException("Binder already registered.", nameof(binder));
            }
            user.ProxiedClient = new ProxiedClient();
            // 3. Join the router session
            _Router.Join(user.ProxiedClient);
            user.Soul = binder.Bind<IConnectionManager>(user.ProxiedClient);
            
        }

        void IBinderProvider.UnregisterClientBinder(IBinder binder)
        {
            if(_Users.TryGetValue(binder, out User user))
            {
                _Users.Remove(binder);
                _Router.Leave(user.ProxiedClient);
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
