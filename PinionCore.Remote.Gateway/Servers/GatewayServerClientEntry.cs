using System.Collections.Generic;
using System.Text;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Servers 
{
    internal class GatewayServerClientEntry : IEntry
    {
        struct BinderInfo
        {
            public IBinder Binder;
            public ISoul Soul;
        }

        readonly IGameLobby _listener;
        readonly System.Collections.Generic.List<BinderInfo> _bindings;
        public GatewayServerClientEntry(IGameLobby gatewayClientListener)
        {
            _bindings = new List<BinderInfo>();
            _listener = gatewayClientListener;
        }
        void IBinderProvider.RegisterClientBinder(IBinder binder)
        {
            _bindings.Add(new BinderInfo
            {
                Binder = binder,
                Soul = binder.Bind<IGameLobby>(_listener)
            });
        }

        void IBinderProvider.UnregisterClientBinder(IBinder binder)
        {
            foreach (var info in _bindings)
            {
                if (info.Binder != binder)
                {                    
                    continue;
                }

                binder.Unbind(info.Soul);
                _bindings.Remove(info);
                break;
            }
        }

        void IEntry.Update()
        {
            
        }
    }
}

