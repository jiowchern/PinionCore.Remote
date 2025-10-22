
using System;
using System.Reactive;
using PinionCore.Remote.Gateway.Hosts;
using PinionCore.Remote.Gateway.Registrys;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway
{
    class Router : IDisposable
    {
        private readonly SessionHub _Hub;
        private readonly Registrys.Server _Registry;
        // 接收遊戲服務 
        public readonly IService Registry;

        // 接收前端用戶
        public readonly IService Session;
        System.Action _Dispose;
        public Router(ISessionSelectionStrategy strategy )
        {
            var hub = new Hosts.SessionHub(strategy);
            var server = new Registrys.Server();

            _Hub = hub;            
            _Registry = server;
            Registry = _Registry.ToService();
            Session = _Hub.Source;
            _Registry.LinesNotifier.Base.Supply += _Supply;
            _Registry.LinesNotifier.Base.Unsupply += _Unsupply;

            _Dispose = () => {
                Registry.Dispose();
                Session.Dispose();
                _Registry.LinesNotifier.Base.Supply -= _Supply;
                _Registry.LinesNotifier.Base.Unsupply -= _Unsupply;
            };
        }

        private void _Unsupply(ILineAllocatable allocatable)
        {
            _Hub.Sink.Unregister(allocatable);
        }

        private void _Supply(ILineAllocatable allocatable)
        {
            _Hub.Sink.Register(allocatable);
        }

        public void Dispose()
        {
            _Dispose();
        }
    }
}



