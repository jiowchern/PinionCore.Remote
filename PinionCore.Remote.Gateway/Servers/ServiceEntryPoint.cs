using System.Collections.Generic;
using System.Text;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Servers 
{
    internal class ServiceEntryPoint : IEntry
    {
        struct BinderInfo
        {
            public IBinder Binder;
            public ISoul Soul;
        }

        readonly IGameLobby _Listener;
        readonly System.Collections.Generic.List<BinderInfo> _Infos;
        public ServiceEntryPoint(IGameLobby gatewayClientListener)
        {
            _Infos = new List<BinderInfo>();
            _Listener = gatewayClientListener;
        }
        void IBinderProvider.RegisterClientBinder(IBinder binder)
        {
            _Infos.Add(new BinderInfo
            {
                Binder = binder,
                Soul = binder.Bind<IGameLobby>(_Listener)
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
