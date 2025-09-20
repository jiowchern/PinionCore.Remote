using PinionCore.Remote.Tools.Protocol.Sources.TestCommon;

namespace PinionCore.Remote.Gateway.Tests
{
    class GameEntry : IEntry , PinionCore.Remote.Tools.Protocol.Sources.TestCommon.IMethodable1
    {
        public GameEntry()
        {
        }

        Value<int> IMethodable1.GetValue1()
        {
            return 1;
        }

        void IBinderProvider.RegisterClientBinder(IBinder binder)
        {
            binder.Bind<IMethodable1>(this);
        }

        void IBinderProvider.UnregisterClientBinder(IBinder binder)
        {
            
        }

        void IEntry.Update()
        {
            
        }
    }
}
