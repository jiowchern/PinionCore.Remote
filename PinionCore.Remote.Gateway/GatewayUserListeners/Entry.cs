using System.Collections.Generic;
using System.Text;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.GatewayUserListeners 
{
    internal class Entry : IEntry
    {
        struct BinderInfo
        {
            public IBinder Binder;
            public ISoul Soul;
        }

        readonly IGatewayUserListener _Listener;
        readonly System.Collections.Generic.List<BinderInfo> _Infos;
        public Entry(IGatewayUserListener gatewayUserListener)
        {
            _Infos = new List<BinderInfo>();
            _Listener = gatewayUserListener;
        }
        void IBinderProvider.RegisterClientBinder(IBinder binder)
        {
            _Infos.Add(new BinderInfo
            {
                Binder = binder,
                Soul = binder.Bind<IGatewayUserListener>(_Listener)
            });
        }

        void IBinderProvider.UnregisterClientBinder(IBinder binder)
        {
            foreach (var info in _Infos)
            {
                if (info.Binder != binder)
                {                    
                    continue;
                }

                binder.Unbind(info.Soul);
                _Infos.Remove(info);
                break;
            }
        }

        void IEntry.Update()
        {
            
        }
    }
}
