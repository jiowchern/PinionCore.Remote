using System.Reactive.Linq;
using PinionCore.Remote.Gateway.Hosts;
using PinionCore.Remote.Gateway.Protocols;
using PinionCore.Remote.Gateway.Servers;
using PinionCore.Remote.Reactive;
using PinionCore.Remote.Soul;
using PinionCore.Remote.Tools.Protocol.Sources.TestCommon;

namespace PinionCore.Remote.Gateway.Tests
{
    public class Tests
    {
        [NUnit.Framework.Test, NUnit.Framework.Timeout(30000)]
        public async System.Threading.Tasks.Task Test()
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
            var userAgent1 = PinionCore.Remote.Gateway.Provider.CreateAgent();
            var userTcpConnector1 = new PinionCore.Network.Tcp.Connector();
            var peer1 = await userTcpConnector1.Connect(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, port1));
            userAgent1.Enable(peer1);
            var agentWorker1 = new AgentWorker(userAgent1);
            agentWorker1.Start();
            var lobby1Obs = from lobby in userAgent1.QueryNotifier<IGameLobby>().SupplyEvent()
                            select lobby;
            var lobby1 = await lobby1Obs.FirstAsync();
            hostHub.Registry.Register(1, lobby1);
            var tcpHostListener1 = new PinionCore.Remote.Server.Tcp.Listener();
            var tcpHostPort1 = PinionCore.Network.Tcp.Tools.GetAvailablePort();
            tcpHostListener1.Bind(tcpHostPort1);
            Soul.IListenable hostListener1 = tcpHostListener1;
            hostListener1.StreamableLeaveEvent += streamable => hostHub.Service.Leave(streamable);
            hostListener1.StreamableEnterEvent += streamable => hostHub.Service.Join(streamable);

            var userAgent2 = PinionCore.Remote.Gateway.Provider.CreateAgent();
            var userTcpConnector2 = new PinionCore.Network.Tcp.Connector();
            var peer2 = await userTcpConnector2.Connect(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, port2));
            userAgent2.Enable(peer2);
            var agentWorker2 = new AgentWorker(userAgent2);
            agentWorker2.Start();
            var lobby2Obs = from lobby in userAgent2.QueryNotifier<IGameLobby>().SupplyEvent()
                            select lobby;
            var lobby2 = await lobby2Obs.FirstAsync();
            hostHub.Registry.Register(2, lobby2);
            var tcpHostListener2 = new PinionCore.Remote.Server.Tcp.Listener();
            var tcpHostPort2 = PinionCore.Network.Tcp.Tools.GetAvailablePort();
            tcpHostListener2.Bind(tcpHostPort2);
            Soul.IListenable hostListener2 = tcpHostListener2;
            hostListener2.StreamableLeaveEvent += streamable => hostHub.Service.Leave(streamable);
            hostListener2.StreamableEnterEvent += streamable => hostHub.Service.Join(streamable);


            // Gateway Host 遊戲服務註冊階段...
            var client1 = new GatewayHostClientAgentPool(PinionCore.Remote.Tools.Protocol.Sources.TestCommon.ProtocolProvider.CreateCase1());
            var tcpClientConnector1 = new PinionCore.Network.Tcp.Connector();
            var clientPeer1 = await tcpClientConnector1.Connect(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, tcpHostPort1));
            client1.Agent.Enable(clientPeer1);
            var clientWorker1 = new AgentWorker(client1.Agent);
            clientWorker1.Start();

            var client2 = new GatewayHostClientAgentPool(PinionCore.Remote.Tools.Protocol.Sources.TestCommon.ProtocolProvider.CreateCase1());
            var tcpClientConnector2 = new PinionCore.Network.Tcp.Connector();
            var clientPeer2 = await tcpClientConnector2.Connect(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, tcpHostPort2));
            client2.Agent.Enable(clientPeer2);
            var clientWorker2 = new AgentWorker(client2.Agent);
            clientWorker2.Start();

            var testValues1 = from agent1 in client1.Agents.SupplyEvent()
                             from m1 in agent1.QueryNotifier<IMethodable1>().SupplyEvent()
                             from m2 in agent1.QueryNotifier<IMethodable2>().SupplyEvent()
                             from m1Value in m1.GetValue1().RemoteValue()
                             from m2Value in m2.GetValue2().RemoteValue()
                                select (m1Value, m2Value);
            NUnit.Framework.Assert.AreEqual((1, 2), await testValues1.FirstAsync());

            var testValues2 = from agent1 in client2.Agents.SupplyEvent()
                              from m1 in agent1.QueryNotifier<IMethodable1>().SupplyEvent()
                              from m2 in agent1.QueryNotifier<IMethodable2>().SupplyEvent()
                              from m1Value in m1.GetValue1().RemoteValue()
                              from m2Value in m2.GetValue2().RemoteValue()
                              select (m1Value, m2Value);
            NUnit.Framework.Assert.AreEqual((1, 2), await testValues2.FirstAsync());


            // releases ...

            clientWorker2.StopAsync().Wait();
            clientWorker1.StopAsync().Wait();
            client2.Agent.Disable();
            client1.Agent.Disable();

            hostHub.Service.Dispose();

            hostListener2.StreamableLeaveEvent -= streamable => hostHub.Service.Leave(streamable);
            hostListener2.StreamableEnterEvent -= streamable => hostHub.Service.Join(streamable);

            hostListener1.StreamableLeaveEvent -= streamable => hostHub.Service.Leave(streamable);
            hostListener1.StreamableEnterEvent -= streamable => hostHub.Service.Join(streamable);

            hostHub.Registry.Unregister(lobby2);
            await agentWorker2.StopAsync();
            userAgent2.Disable();
            listener2.StreamableLeaveEvent -= streamable => service2.Leave(streamable);
            listener2.StreamableEnterEvent -= streamable => service2.Join(streamable);
            service2.Dispose();
            tcpListener2.Close();
            serviceHub2.Service.Dispose();

            hostHub.Registry.Unregister(lobby1);
            await agentWorker1.StopAsync();
            userAgent1.Disable();
            listener1.StreamableLeaveEvent -= streamable => service1.Leave(streamable);
            listener1.StreamableEnterEvent -= streamable => service1.Join(streamable);
            service1.Dispose();
            tcpListener1.Close();
            serviceHub1.Service.Dispose();
        }
    }
}



