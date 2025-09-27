using PinionCore.Utility;
using PinionCore.Remote;
using PinionCore.Consoles.Chat1.Common;
using System;

namespace PinionCore.Consoles.Chat1
{
    internal class UserLogin : IBootable , ILogin
    {
        private IBinder _Binder;
        private ISoul _Login;

        public UserLogin(IBinder binder)
        {
            _Binder = binder;        
        }
        public event System.Action<string> DoneEvent;
        

        void IBootable.Launch()
        {
            _Login = _Binder.Bind<ILogin>(this);
        }

        Value<bool> ILogin.Login(string name)
        {
            DoneEvent(name);
            return true;
        }

        void IBootable.Shutdown()
        {
            _Binder.Unbind(_Login);
        }
    }
}
