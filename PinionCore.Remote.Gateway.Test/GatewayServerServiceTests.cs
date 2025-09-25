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

            var gameEntry2 = new TestGameEntry(TestGameEntry.GameType.Method2);
            Soul.IService service2 = PinionCore.Remote.Standalone.Provider.CreateService(gameEntry2, PinionCore.Remote.Tools.Protocol.Sources.TestCommon.ProtocolProvider.CreateCase1());
            var serviceHub2 = new GatewayServerServiceHub();
            var tcpListener2 = new PinionCore.Remote.Server.Tcp.Listener();
            Soul.IListenable listener2 = tcpListener2;
            listener2.StreamableLeaveEvent += streamable => serviceHub2.Service.Leave(streamable);
            listener2.StreamableEnterEvent += streamable => serviceHub2.Service.Join(streamable);
            serviceHub2.Listener.StreamableLeaveEvent += streamable => service2.Leave(streamable);
            serviceHub2.Listener.StreamableEnterEvent += streamable => service2.Join(streamable);
            var port2 = PinionCore.Network.Tcp.Tools.GetAvailablePort();
            tcpListener2.Bind(port2);

            // Gateway Host 連線階段...
            var hostHub = new GatewayHostServiceHub();

            // Set up TCP listener for Gateway Host first
            var tcpHostListener = new PinionCore.Remote.Server.Tcp.Listener();
            var tcpHostPort = PinionCore.Network.Tcp.Tools.GetAvailablePort();
            tcpHostListener.Bind(tcpHostPort);
            Soul.IListenable hostListener = tcpHostListener;
            hostListener.StreamableLeaveEvent += streamable => hostHub.Service.Leave(streamable);
            hostListener.StreamableEnterEvent += streamable => hostHub.Service.Join(streamable);

            // Connect Gateway Host to Game Service 1
            var userAgent1 = PinionCore.Remote.Gateway.Provider.CreateAgent();
            var userTcpConnector1 = new PinionCore.Network.Tcp.Connector();
            var peer1 = await userTcpConnector1.Connect(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, port1));
            userAgent1.Enable(peer1);
            var agentWorker1 = new AgentWorker(userAgent1);
            agentWorker1.Start();
            var lobby1Obs = from lobby in userAgent1.QueryNotifier<IGameLobby>().SupplyEvent()
                            select lobby;
            var lobby1 = await lobby1Obs.FirstAsync();
            var disposer1 = new ClientConnectionDisposer(new RoundRobinGameLobbySelectionStrategy());
            disposer1.Add(lobby1);
            hostHub.Registry.Register(1, disposer1);

            // Connect Gateway Host to Game Service 2
            var userAgent2 = PinionCore.Remote.Gateway.Provider.CreateAgent();
            var userTcpConnector2 = new PinionCore.Network.Tcp.Connector();
            var peer2 = await userTcpConnector2.Connect(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, port2));
            userAgent2.Enable(peer2);
            var agentWorker2 = new AgentWorker(userAgent2);
            agentWorker2.Start();
            var lobby2Obs = from lobby in userAgent2.QueryNotifier<IGameLobby>().SupplyEvent()
                            select lobby;
            var lobby2 = await lobby2Obs.FirstAsync();
            var disposer2 = new ClientConnectionDisposer(new RoundRobinGameLobbySelectionStrategy());
            disposer2.Add(lobby2);
            hostHub.Registry.Register(2, disposer2);


            // Client connections to Gateway Host
            var client1 = new GatewayHostClientAgentPool(PinionCore.Remote.Tools.Protocol.Sources.TestCommon.ProtocolProvider.CreateCase1());
            var tcpClientConnector1 = new PinionCore.Network.Tcp.Connector();
            var clientPeer1 = await tcpClientConnector1.Connect(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, tcpHostPort));
            client1.Agent.Enable(clientPeer1);
            var clientWorker1 = new AgentWorker(client1.Agent);
            clientWorker1.Start();


            var agentsObs1 = from agent in client1.Agents.Base.SupplyEvent()
                             select agent;

            var agents1 = await agentsObs1.Buffer(2).FirstAsync();
            var testValue1 = 0;
            foreach(var agent1 in agents1)
            {
                agent1.QueryNotifier<IMethodable1>().Supply += m1 =>
                {
                    var value = m1.GetValue1();

                    value.OnValue += v => {
                        testValue1 += v;
                    };
                };
                agent1.QueryNotifier<IMethodable2>().Supply += m2 => {
                    var value = m2.GetValue1();

                    value.OnValue += v => {
                        testValue1 += v;
                    };
                };
            }
                

            while(testValue1 < 3)
            {
                foreach(var agent1 in agents1)
                {
                    agent1.HandlePackets();
                    agent1.HandleMessage();
                }
                
                await System.Threading.Tasks.Task.Delay(100);
            }


            // Test that both clients can access both game services through the Gateway Host
            var m1Results = from agent in client1.Agents.Base.SupplyEvent()
                           from m1 in agent.QueryNotifier<IMethodable1>().SupplyEvent()
                           from value in m1.GetValue1().RemoteValue()
                           select value;

            var m2Results = from agent in client1.Agents.Base.SupplyEvent()
                           from m2 in agent.QueryNotifier<IMethodable2>().SupplyEvent()
                           from value in m2.GetValue2().RemoteValue()
                           select value;

            var result1 = await m1Results.FirstAsync();
            var result2 = await m2Results.FirstAsync();

            NUnit.Framework.Assert.AreEqual(1, result1);
            NUnit.Framework.Assert.AreEqual(2, result2);
           


            // releases ...

            // Stop client workers first            
            await clientWorker1.StopAsync();

            // Disable client agents            
            client1.Agent.Disable();

            // Unregister from host hub before disposing
            disposer2.Remove(lobby2);
            hostHub.Registry.Unregister(disposer2);
            disposer1.Remove(lobby1);
            hostHub.Registry.Unregister(disposer1);

            // Disconnect event handlers for host listener
            hostListener.StreamableLeaveEvent -= streamable => hostHub.Service.Leave(streamable);
            hostListener.StreamableEnterEvent -= streamable => hostHub.Service.Join(streamable);

            // Close TCP host listener
            tcpHostListener.Close();

            // Dispose host hub service
            hostHub.Service.Dispose();

            // Stop and disable user agent 2
            await agentWorker2.StopAsync();
            userAgent2.Disable();

            // Disconnect service 2 event handlers
            listener2.StreamableLeaveEvent -= streamable => serviceHub2.Service.Leave(streamable);
            listener2.StreamableEnterEvent -= streamable => serviceHub2.Service.Join(streamable);
            serviceHub2.Listener.StreamableLeaveEvent -= streamable => service2.Leave(streamable);
            serviceHub2.Listener.StreamableEnterEvent -= streamable => service2.Join(streamable);

            // Dispose service 2 resources
            serviceHub2.Service.Dispose();
            service2.Dispose();
            tcpListener2.Close();

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



