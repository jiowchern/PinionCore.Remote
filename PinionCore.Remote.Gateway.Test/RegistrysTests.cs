using System.Net;
using NUnit.Framework;
using PinionCore.Remote.Gateway.Servers;
using PinionCore.Remote.Soul;


namespace PinionCore.Remote.Gateway.Tests
{
    public class RegistrysTests
    {
        [NUnit.Framework.Test, Timeout(10000)]
        public async System.Threading.Tasks.Task ProviderRegistry_Connect_EventHandling_Test()
        {

            var hub1 = new Servers.ServiceHub();
            var tcpListener = new PinionCore.Remote.Server.Tcp.Listener();
            var port1 = PinionCore.Network.Tcp.Tools.GetAvailablePort();
            IListenable listenable = tcpListener;
            listenable.StreamableEnterEvent += hub1.Source.Join;
            listenable.StreamableLeaveEvent += hub1.Source.Leave;
            tcpListener.Bind(port1);

            var registry = new PinionCore.Remote.Gateway.Registrys.ProviderRegistry();

            var mre1 = new System.Threading.ManualResetEvent(false);
            registry.ProviderAddedEvent += (g,s) => {
                mre1.Set();
            };
            registry.Connect(1 ,  IPEndPoint.Parse($"127.0.0.1:{port1}"));

            mre1.WaitOne(2000);

            // release...
            listenable.StreamableEnterEvent -= hub1.Source.Join;
            listenable.StreamableLeaveEvent -= hub1.Source.Leave;
            tcpListener.Close();

            
        }
    }
}

