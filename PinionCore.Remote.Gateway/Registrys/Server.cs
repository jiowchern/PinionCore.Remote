using System.Collections.Generic;
using PinionCore.Remote.Gateway.Protocols;
using PinionCore.Remote.Soul;
using PinionCore.Utility;

namespace PinionCore.Remote.Gateway.Registrys
{
    class Server : PinionCore.Remote.IEntry, System.IDisposable
    {
        readonly PinionCore.Utility.Updater _Updater;
        readonly Depot<ILineAllocatable> _Lines;
        public readonly Notifier<ILineAllocatable> LinesNotifier;

        readonly Dictionary<IBinder, User> _Users;
        public Server()
        {
            _Updater = new Updater();
            _Lines = new Depot<ILineAllocatable>();
            LinesNotifier = new Notifier<ILineAllocatable>(_Lines);
            _Users = new Dictionary<IBinder, User>();
        }
        void IBinderProvider.RegisterClientBinder(IBinder binder)
        {
            var user = new User(binder, _Lines);
            _Users.Add(binder, user);
            _Updater.Add(user);
        }

        void IBinderProvider.UnregisterClientBinder(IBinder binder)
        {
            if (!_Users.ContainsKey(binder))
            {
                return;
            }
            var user = _Users[binder];            
            user.Dispose();
            _Users.Remove(binder);
            _Updater.Remove(user);
        }

        void IEntry.Update()
        {
            _Updater.Working();
        }

        public IService ToService()
        {
            return PinionCore.Remote.Standalone.Provider.CreateService(this, ProtocolProvider.Create());
        }

        public void Dispose()
        {
            _Updater.Shutdown();
        }
    }
}

