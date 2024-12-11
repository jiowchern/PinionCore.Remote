using System;

namespace PinionCore.Remote.Soul
{
    public class SyncService : System.IDisposable
    {
        readonly IEntry _Entry;
        readonly UserProvider _UserProvider;
        readonly IDisposable _UserProviderDisposable;



        public SyncService(IEntry entry, UserProvider user_provider)
        {
            _Entry = entry;
            _UserProvider = user_provider;
            _UserProviderDisposable = user_provider;
        }

        public void Update()
        {

            while (_UserProvider.UserLifecycleEvents.TryTake(out UserProvider.UserLifecycleEvent userAction))
            {
                if (userAction.State == UserProvider.UserLifecycleState.Join)
                {
                    _Entry.RegisterClientBinder(userAction.User.Binder);
                }
                else if (userAction.State == UserProvider.UserLifecycleState.Leave)
                {
                    _Entry.UnregisterClientBinder(userAction.User.Binder);
                }

            }

            _Entry.Update();


            foreach (Advanceable user in _UserProvider.Users.Values)
            {
                try
                {
                    user.Advance();
                }
                catch (Exception e)
                {
                    PinionCore.Utility.Log.Instance.WriteInfo($"Advance Error: {e.ToString()}");
                }
            }

        }

        void IDisposable.Dispose()
        {
            _UserProviderDisposable.Dispose();
        }
    }
}

