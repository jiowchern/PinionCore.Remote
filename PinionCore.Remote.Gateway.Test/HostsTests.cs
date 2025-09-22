
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;



using NUnit.Framework;

using PinionCore.Remote.Gateway.Hosts;
using PinionCore.Remote.Gateway.Protocols;
using PinionCore.Remote.Reactive;
using PinionCore.Remote.Standalone;
using PinionCore.Remote.Tools.Protocol.Sources.TestCommon;



namespace PinionCore.Remote.Gateway.Tests
{
    public class HostsTests
    {
        [NUnit.Framework.Test, Timeout(10000)]
        public async System.Threading.Tasks.Task UserSessionSetAndLeaveTest()
        {
            var listener1 = new PinionCore.Remote.Gateway.Servers.ConnectionListener();
            PinionCore.Remote.Gateway.Protocols.IGameLobby service1 = listener1;
            

            var listener2 = new PinionCore.Remote.Gateway.Servers.ConnectionListener();
            PinionCore.Remote.Gateway.Protocols.IGameLobby service2 = listener2;

            var user1 = new PinionCore.Remote.Gateway.Hosts.ProxiedClient();
            IRoutableSession session1 = user1;
            IConnectionManager owner1 = user1;

            var userSetObs1 = from userId in service1.Join().RemoteValue()
                       from clientConnection in service1.ClientNotifier.Base.SupplyEvent()
                       where clientConnection.Id == userId
                       select new { userId , Result = session1.Set(1, clientConnection) };

            var setResult1 = await userSetObs1.FirstAsync();
            Assert.IsTrue(setResult1.Result);

            var userSetObs2 = from userId in service2.Join().RemoteValue()
                           from clientConnection in service2.ClientNotifier.Base.SupplyEvent()
                           where clientConnection.Id == userId
                           select new { userId , Result = session1.Set(1, clientConnection) } ;
            var setResult2 = await userSetObs2.FirstAsync();
            Assert.IsFalse(setResult2.Result);


            var service2LeaveObs = from clientConnection in service2.ClientNotifier.Base.UnsupplyEvent()
                               where clientConnection.Id == setResult2.userId
                                select service2.ClientNotifier.Collection.Count;

            var count2 = new int?();
            service2LeaveObs.Subscribe(c => count2 = c);
            var service2LeaveResult = await service2.Leave(setResult2.userId);
            Assert.AreEqual(ResponseStatus.Success, service2LeaveResult);
            while (!count2.HasValue)
                await System.Threading.Tasks.Task.Delay(10);

            Assert.AreEqual(0 , count2.Value);

            var service1LeaveObs = from clientConnection in service1.ClientNotifier.Base.UnsupplyEvent()
                                   where clientConnection.Id == setResult1.userId
                                   select service2.ClientNotifier.Collection.Count;

            var count1 = new int?();
            service1LeaveObs.Subscribe(c => count1 = c);
            var service1LeaveResult = await service1.Leave(setResult1.userId);
            Assert.AreEqual(ResponseStatus.Success, service1LeaveResult);
            while (!count1.HasValue)
                await System.Threading.Tasks.Task.Delay(10);

            Assert.AreEqual(0, count1.Value);
        }
        
        [NUnit.Framework.Test, Timeout(10000)]
        public void UserSessionRouteManagementTest()
        {
            var route = new PinionCore.Remote.Gateway.Hosts.SessionOrchestrator();
            var user1 = new PinionCore.Remote.Gateway.Hosts.ProxiedClient();
            IConnectionManager owner1 = user1;

            var listener1 = new PinionCore.Remote.Gateway.Servers.ConnectionListener();
            PinionCore.Remote.Gateway.Protocols.IGameLobby service1 = listener1;

            var listener2 = new PinionCore.Remote.Gateway.Servers.ConnectionListener();
            PinionCore.Remote.Gateway.Protocols.IGameLobby service2 = listener2;
            var listener3 = new PinionCore.Remote.Gateway.Servers.ConnectionListener();
            PinionCore.Remote.Gateway.Protocols.IGameLobby service3 = listener3;

            route.Join(user1);
            Assert.AreEqual(0, owner1.Connections.Collection.Count);
            route.Register(1, service1);
            Assert.AreEqual(1, owner1.Connections.Collection.Count);

            route.Register(1, service2);
            Assert.AreEqual(1, owner1.Connections.Collection.Count);
            route.Unregister(service1);
            Assert.AreEqual(1, owner1.Connections.Collection.Count);

            route.Register(2, service3);
            Assert.AreEqual(2, owner1.Connections.Collection.Count);

            route.Leave(user1);

            var user2 = new PinionCore.Remote.Gateway.Hosts.ProxiedClient();
            IConnectionManager owner2 = user2;
            route.Join(user2);

            Assert.AreEqual(2, owner2.Connections.Collection.Count);
        }        

        [NUnit.Framework.Test, Timeout(10000)]
        public async System.Threading.Tasks.Task GatewayCoordinatorAgentWorkflowTest()
        {
            var gameEntry = new GameEntry();
            var gameProtocol = PinionCore.Remote.Tools.Protocol.Sources.TestCommon.ProtocolProvider.CreateCase1();

            // 建立真正的遊戲服務
            PinionCore.Remote.Soul.IService actualGameService = Standalone.Provider.CreateService(gameEntry, gameProtocol);

            // 建立連線服務
            var connectionService = new PinionCore.Remote.Gateway.Servers.ConnectionService();

            // 掛接 Join/Leave 事件來連結遊戲服務
            connectionService.Listener.StreamableEnterEvent += streamable => actualGameService.Join(streamable);
            connectionService.Listener.StreamableLeaveEvent += streamable => actualGameService.Leave(streamable);

            // 建立host 監聽的服務 agent
            var userAgent = PinionCore.Remote.Gateway.Provider.CreateAgent();
            var userAgentDisconnect = userAgent.Connect(connectionService.Service);
            var userAgentWorker = new AgentWorker(userAgent);
            userAgentWorker.Start();

            // 取得遊戲服務
            var gameObs = from _game in userAgent.QueryNotifier<IGameLobby>().SupplyEvent()
                          select _game;
            var game = await gameObs.FirstAsync();

            // 建立 gateway host
            var gatewayHost = new PinionCore.Remote.Gateway.Hosts.GatewayCoordinator();

            // 1. 註冊遊戲服務
            gatewayHost.Registry.Register(1, game);

            // 建立 gateway agent
            var gatewayAgent = new PinionCore.Remote.Gateway.Hosts.ClientProxy(gameProtocol);
            // 2. 連線到 gateway host
            gatewayAgent.Agent.Connect(gatewayHost.Service);
            var gatewayAgentWorker = new AgentWorker(gatewayAgent.Agent);
            gatewayAgentWorker.Start();

            // 取得遊戲服務的 agent
            var gameAgentObs = from _gameAgent in gatewayAgent.Agents.Base.SupplyEvent()                            
                            select _gameAgent;
            var gameAgent = await gameAgentObs.FirstAsync();
            var gameAgentWorker = new AgentWorker(gameAgent);
            gameAgentWorker.Start();

            var valueObs = from _m1 in gameAgent.QueryNotifier<IMethodable1>().SupplyEvent()
                           from _value in _m1.GetValue1().RemoteValue()
                           select _value;
            var value = await valueObs.FirstAsync();
            Assert.AreEqual(1, value);


            await gameAgentWorker.StopAsync();
            await gatewayAgentWorker.StopAsync();
            gatewayHost.Registry.Unregister(game);
            gatewayHost.Service.Dispose();
            await userAgentWorker.StopAsync();

            // 清理資源
            connectionService.Listener.StreamableEnterEvent -= streamable => actualGameService.Join(streamable);
            connectionService.Listener.StreamableLeaveEvent -= streamable => actualGameService.Leave(streamable);

            userAgentDisconnect();
            connectionService.Dispose();
            actualGameService.Dispose();
        }
     }
}
