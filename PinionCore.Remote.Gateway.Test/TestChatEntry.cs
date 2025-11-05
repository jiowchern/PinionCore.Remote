using System;
using PinionCore.Consoles.Chat1.Common;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway.Tests
{
    class TestChatEntry : IEntry , PinionCore.Consoles.Chat1.Common.ILogin
    {
        internal IService ToService()
        {
            return new PinionCore.Remote.Soul.Service(this, PinionCore.Consoles.Chat1.Common.ProtocolCreator.Create());
        }

        Value<bool> ILogin.Login(string name)
        {
            return true;
        }

        void IBinderProvider.RegisterClientBinder(IBinder binder)
        {
            binder.Bind<ILogin>(this);
        }

        void IBinderProvider.UnregisterClientBinder(IBinder binder)
        {
            
        }

        void IEntry.Update()
        {
            
        }
    }
}
