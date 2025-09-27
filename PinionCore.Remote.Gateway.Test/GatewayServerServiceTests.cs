using System.Linq;
using System.Reactive.Linq;
using PinionCore.Remote.Gateway.Hosts;
using PinionCore.Remote.Gateway.Protocols;
using PinionCore.Remote.Gateway.Servers;
using PinionCore.Remote.Reactive;
using PinionCore.Remote.Soul;
using PinionCore.Remote.Tools.Protocol.Sources.TestCommon;

namespace PinionCore.Remote.Gateway.Tests
{
    public class GatewayServerServiceTests
    {
        [NUnit.Framework.Test, NUnit.Framework.Timeout(30000)]
        public async System.Threading.Tasks.Task GatewayHostServiceIntegrationTest()
        {
            // 遊戲服務建立階段...
            var gameEntry1 = new TestGameEntry(TestGameEntry.GameType.Method1);
            Soul.IService service1 = PinionCore.Remote.Standalone.Provider.CreateService(gameEntry1, PinionCore.Remote.Tools.Protocol.Sources.TestCommon.ProtocolProvider.CreateCase1());
            var serviceHub1 = new GatewayServerServiceHub();            
            var tcpListener1 = new PinionCore.Remote.Server.Tcp.Listener();
            Soul.IListenable listener1 = tcpListener1;
            listener1.StreamableLeaveEvent += streamable => serviceHub1.Service.Leave(streamable);
            listener1.StreamableEnterEvent += streamable => serviceHub1.Service.Join(streamable);
            serviceHub1.Listener.StreamableLeaveEvent += streamable => service1.Leave(streamable);
            serviceHub1.Listener.StreamableEnterEvent += streamable => service1.Join(streamable);
            var port1 = PinionCore.Network.Tcp.Tools.GetAvailablePort();
            tcpListener1.Bind(port1);

            

            // Gateway Host 連線階段...
            var hostHub = new GatewayHostServiceHub(new RoundRobinGameLobbySelectionStrategy());

            // Set up TCP listener for Gateway Host first
            var tcpHostListener = new PinionCore.Remote.Server.Tcp.Listener();
            var tcpHostPort = PinionCore.Network.Tcp.Tools.GetAvailablePort();
            tcpHostListener.Bind(tcpHostPort);
            Soul.IListenable hostListener = tcpHostListener;
            hostListener.StreamableLeaveEvent += streamable => hostHub.Service.Leave(streamable);
            hostListener.StreamableEnterEvent += streamable => hostHub.Service.Join(streamable);

            // Connect Gateway Host to Game Service 1
            var userAgent1 = Provider.CreateAgent();
            var userTcpConnector1 = new PinionCore.Network.Tcp.Connector();
            var peer1 = await userTcpConnector1.Connect(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, port1));
            userAgent1.Enable(peer1);
            var agentWorker1 = new AgentWorker(userAgent1);
            agentWorker1.Start();
            var lobby1Obs = from lobby in userAgent1.QueryNotifier<IGameLobby>().SupplyEvent()
                            select lobby;
            var lobby1 = await lobby1Obs.FirstAsync();
            
            hostHub.Registry.Register(1, lobby1);

            


            // Client connections to Gateway Host
            var client1 = new GatewayHostClientAgentPool(PinionCore.Remote.Tools.Protocol.Sources.TestCommon.ProtocolProvider.CreateCase1());
            var tcpClientConnector1 = new PinionCore.Network.Tcp.Connector();
            var clientPeer1 = await tcpClientConnector1.Connect(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, tcpHostPort));
            client1.Agent.Enable(clientPeer1);
            var clientWorker1 = new AgentWorker(client1.Agent);
            clientWorker1.Start();


            var agentsObs1 = from agent in client1.Agents.Base.SupplyEvent()
                             select agent;

            var agent1 = await agentsObs1.FirstAsync();
            var agent1Worker = new AgentWorker(agent1);
            agent1Worker.Start();

            // Test that both clients can access both game services through the Gateway Host
            var m1Results = from agent in client1.Agents.Base.SupplyEvent()
                           from m1 in agent.QueryNotifier<IMethodable1>().SupplyEvent()
                           from value in m1.GetValue1().RemoteValue()
                           select value;

           

            var result1 = await m1Results.FirstAsync();
           

            NUnit.Framework.Assert.AreEqual(1, result1);


            // releases ...

            // Stop client workers first            
            await clientWorker1.StopAsync();

            // Disable client agents            
            client1.Agent.Disable();

            // Unregister from host hub before disposing
            
            
            
            hostHub.Registry.Unregister(1 , lobby1);

            // Disconnect event handlers for host listener
            hostListener.StreamableLeaveEvent -= streamable => hostHub.Service.Leave(streamable);
            hostListener.StreamableEnterEvent -= streamable => hostHub.Service.Join(streamable);

            // Close TCP host listener
            tcpHostListener.Close();

            // Dispose host hub service
            hostHub.Service.Dispose();

            // Stop and disable user agent 1
            await agentWorker1.StopAsync();
            userAgent1.Disable();

            // Disconnect service 1 event handlers
            listener1.StreamableLeaveEvent -= streamable => serviceHub1.Service.Leave(streamable);
            listener1.StreamableEnterEvent -= streamable => serviceHub1.Service.Join(streamable);
            serviceHub1.Listener.StreamableLeaveEvent -= streamable => service1.Leave(streamable);
            serviceHub1.Listener.StreamableEnterEvent -= streamable => service1.Join(streamable);

            // Dispose service 1 resources
            serviceHub1.Service.Dispose();
            service1.Dispose();
            tcpListener1.Close();
        }
    }
}



