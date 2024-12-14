using PinionCore.Remote;
using PinionCore.Samples.HelloWorld.Protocols;

namespace PinionCore.Samples.HelloWorld.Server
{
    internal class Entry : PinionCore.Remote.IEntry
    {
        public volatile bool Enable;

        readonly Greeter _Greeter;
        public Entry()
        {
            _Greeter = new Greeter();
            Enable = true;
        }

        void IBinderProvider.RegisterClientBinder(PinionCore.Remote.IBinder binder)
        {
            // IBinder is what you get when your client completes the connection.            
            var soul = binder.Bind<IGreeter>(_Greeter);
            // unbind : binder.Unbind<IGreeter>(soul);
        }



        private void _End()
        {
            Enable = false;
        }

        void IEntry.Update()
        {
            
        }

        void IBinderProvider.UnregisterClientBinder(IBinder binder)
        {
            _End();
        }
    }
}
