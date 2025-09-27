using System;
using System.Net;

using PinionCore.Remote.Gateway.Protocols;


namespace PinionCore.Remote.Gateway.Registrys 
{
    
    public class ProviderRegistry :System.IDisposable
    {
        private readonly IProtocol _Protocol;

        public event System.Action<uint, IConnectionProvider> AddProviderEvent;
        public event System.Action<uint, IConnectionProvider> RemoveProviderEvent;
        public ProviderRegistry()
        {
            _Protocol = PinionCore.Remote.Gateway.Protocols.ProtocolProvider.Create();
        }

        public void Connect(uint id, EndPoint endPoint)
        {
            var set = PinionCore.Remote.Client.Provider.CreateTcpAgent(_Protocol);
            throw new System.NotImplementedException();
        }

        void IDisposable.Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
