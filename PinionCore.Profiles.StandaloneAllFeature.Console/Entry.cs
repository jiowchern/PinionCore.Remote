using PinionCore.Profiles.StandaloneAllFeature.Protocols;
using PinionCore.Remote;
using System.Diagnostics;

namespace PinionCore.Profiles.StandaloneAllFeature.Server
{
    class Entry : PinionCore.Remote.IEntry 
    {
        readonly System.Collections.Generic.List<User> _Users;

        public Entry()
        {
            _Users = new List<User>();
        }

        void IBinderProvider.RegisterClientBinder(IBinder binder)
        {            
            
            var user = new User(binder);
            _Users.Add(user);
            
        }

        void IBinderProvider.UnregisterClientBinder(IBinder binder)
        {
            _Users.RemoveAll(user => user.Binder == binder);
        }

        void IEntry.Update()
        {
            
        }
    }
}
