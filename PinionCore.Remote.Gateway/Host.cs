
using System;
using System.Reactive;
using PinionCore.Remote.Gateway.Hosts;
using PinionCore.Remote.Gateway.Registrys;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway
{
    class Host : IDisposable
    {
        private readonly ServiceHub _Hub;
        private readonly Registrys.Server _Registry;
        // 接收遊戲服務 
        public readonly IService RegistryService;

        // 接收前端用戶
        public readonly IService HubService;
        System.Action _Dispose;
        public Host(ISessionSelectionStrategy strategy )
        {
            var hub = new Hosts.ServiceHub(strategy);
            var server = new Registrys.Server();

            _Hub = hub;            
            _Registry = server;
            RegistryService = _Registry.ToService();
            HubService = _Hub.Source;
            _Registry.LinesNotifier.Base.Supply += _Supply;
            _Registry.LinesNotifier.Base.Unsupply += _Unsupply;

            _Dispose = () => {
                RegistryService.Dispose();
                HubService.Dispose();
                _Registry.LinesNotifier.Base.Supply -= _Supply;
                _Registry.LinesNotifier.Base.Unsupply -= _Unsupply;
            };
        }

        private void _Unsupply(ILineAllocatable allocatable)
        {
            _Hub.Sink.Unregister(allocatable.Group, allocatable);
        }

        private void _Supply(ILineAllocatable allocatable)
        {
            _Hub.Sink.Register(allocatable.Group, allocatable);
        }

        public void Dispose()
        {
            _Dispose();
        }
    }
}



