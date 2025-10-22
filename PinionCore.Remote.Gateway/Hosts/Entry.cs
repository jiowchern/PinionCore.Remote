using System;
using System.Net.Http.Headers;
using PinionCore.Network;
using PinionCore.Remote.Gateway.Protocols;
using PinionCore.Utility;

namespace PinionCore.Remote.Gateway.Hosts
{
    internal class Entry : IEntry  ,IDisposable
    {

        private readonly ISessionMembershipProvider _provider;
        readonly System.Collections.Generic.Dictionary<IBinder, User> _Users;
        readonly PinionCore.Utility.Updater _Updater;

        public Entry(ISessionMembershipProvider provider)
        {
            _Updater = new PinionCore.Utility.Updater();
            _provider = provider;
            _Users = new System.Collections.Generic.Dictionary<IBinder, User>();
        }

        public void Dispose()
        {
            _Updater.Shutdown();
        }

        void IBinderProvider.RegisterClientBinder(IBinder binder)
        {
            var user = new User(binder,_provider);            
            if (!_Users.TryAdd(binder, user))
            {
                throw new ArgumentException("Binder already registered.", nameof(binder));
            }
            _Updater.Add(user);
            
        }

        void IBinderProvider.UnregisterClientBinder(IBinder binder)
        {
            if(_Users.TryGetValue(binder, out User user))
            {
                _Updater.Remove(user);
                _Users.Remove(binder);
                //_sessionMembership.Leave(user.ConnectionManager);
                
            }
            else
            {
                throw new ArgumentException("Binder not registered.", nameof(binder));
            }
        }

        void IEntry.Update()
        {
            _Updater.Working();
        }
    }
}


