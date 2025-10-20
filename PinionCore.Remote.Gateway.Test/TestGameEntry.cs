using PinionCore.Remote.Soul;
using PinionCore.Remote.Tools.Protocol.Sources.TestCommon;

namespace PinionCore.Remote.Gateway.Tests
{
    class TestGameEntry : IEntry ,
        PinionCore.Remote.Tools.Protocol.Sources.TestCommon.IMethodable1,
        PinionCore.Remote.Tools.Protocol.Sources.TestCommon.IMethodable2

    {
        private readonly GameType _GameType;

        public enum GameType
        {
            Method1,
            Method2
        }
        public TestGameEntry(GameType gameType)
        {
            _GameType = gameType;
        }

        Value<int> IMethodable1.GetValue1()
        {
            return 1;
        }

        Value<int> IMethodable2.GetValue2()
        {
            return 2;
        }

        void IBinderProvider.RegisterClientBinder(IBinder binder)
        {
            if(_GameType == GameType.Method1)
                binder.Bind<IMethodable1>(this);
            else
                binder.Bind<IMethodable2>(this);
        }

        Value<HelloReply> IMethodable2.SayHello(HelloRequest request)
        {
            return new HelloReply { Message = "Hello " + request.Name };
        }

        void IBinderProvider.UnregisterClientBinder(IBinder binder)
        {
            
        }

        void IEntry.Update()
        {
            
        }

        public IService ToService()
        {            
            return PinionCore.Remote.Standalone.Provider.CreateService(this, PinionCore.Remote.Tools.Protocol.Sources.TestCommon.ProtocolProvider.CreateCase1());
        }
    }
}
