
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
            var listener1 = new PinionCore.Remote.Gateway.Servers.Listener();
            PinionCore.Remote.Gateway.Protocols.IGameService service1 = listener1;
            

            var listener2 = new PinionCore.Remote.Gateway.Servers.Listener();
            PinionCore.Remote.Gateway.Protocols.IGameService service2 = listener2;

            var user1 = new PinionCore.Remote.Gateway.Hosts.ClientUser();
            ISession session1 = user1;
            IServiceSessionOwner owner1 = user1;

            var userSetObs1 = from userId in service1.Join().RemoteValue()
                       from serviceUser in service1.UserNotifier.Base.SupplyEvent()
                       where serviceUser.Id == userId
                       select new { userId , Result = session1.Set(1, serviceUser) };

            var setResult1 = await userSetObs1.FirstAsync();
            Assert.IsTrue(setResult1.Result);

            var userSetObs2 = from userId in service2.Join().RemoteValue()
                           from serviceUser in service2.UserNotifier.Base.SupplyEvent()
                           where serviceUser.Id == userId
                           select new { userId , Result = session1.Set(1, serviceUser) } ;
            var setResult2 = await userSetObs2.FirstAsync();
            Assert.IsFalse(setResult2.Result);


            var service2LeaveObs = from serviceUser in service2.UserNotifier.Base.UnsupplyEvent()
                               where serviceUser.Id == setResult2.userId
                                select service2.UserNotifier.Collection.Count;

            var count2 = new int?();
            service2LeaveObs.Subscribe(c => count2 = c);
            var service2LeaveResult = await service2.Leave(setResult2.userId);
            Assert.AreEqual(ReturnCode.Success, service2LeaveResult);
            while (!count2.HasValue)
                await System.Threading.Tasks.Task.Delay(10);

            Assert.AreEqual(0 , count2.Value);

            var service1LeaveObs = from serviceUser in service1.UserNotifier.Base.UnsupplyEvent()
                                   where serviceUser.Id == setResult1.userId
                                   select service2.UserNotifier.Collection.Count;

            var count1 = new int?();
            service1LeaveObs.Subscribe(c => count1 = c);
            var service1LeaveResult = await service1.Leave(setResult1.userId);
            Assert.AreEqual(ReturnCode.Success, service1LeaveResult);
            while (!count1.HasValue)
                await System.Threading.Tasks.Task.Delay(10);

            Assert.AreEqual(0, count1.Value);
        }
        
        [NUnit.Framework.Test, Timeout(10000)]
        public void UserSessionRouteManagementTest()
        {
            var route = new PinionCore.Remote.Gateway.Hosts.Router();
            var user1 = new PinionCore.Remote.Gateway.Hosts.ClientUser();
            IServiceSessionOwner owner1 = user1;

            var listener1 = new PinionCore.Remote.Gateway.Servers.Listener();
            PinionCore.Remote.Gateway.Protocols.IGameService service1 = listener1;

            var listener2 = new PinionCore.Remote.Gateway.Servers.Listener();
            PinionCore.Remote.Gateway.Protocols.IGameService service2 = listener2;
            var listener3 = new PinionCore.Remote.Gateway.Servers.Listener();
            PinionCore.Remote.Gateway.Protocols.IGameService service3 = listener3;

            route.Join(user1);
            Assert.AreEqual(0, owner1.Sessions.Collection.Count);
            route.Register(1, service1);
            Assert.AreEqual(1, owner1.Sessions.Collection.Count);

            route.Register(1, service2);
            Assert.AreEqual(1, owner1.Sessions.Collection.Count);
            route.Unregister(service1);
            Assert.AreEqual(1, owner1.Sessions.Collection.Count);

            route.Register(2, service3);
            Assert.AreEqual(2, owner1.Sessions.Collection.Count);

            route.Leave(user1);

            var user2 = new PinionCore.Remote.Gateway.Hosts.ClientUser();
            IServiceSessionOwner owner2 = user2;
            route.Join(user2);

            Assert.AreEqual(2, owner2.Sessions.Collection.Count);
        }        

        [NUnit.Framework.Test, Timeout(10000)]
        public async System.Threading.Tasks.Task GatewayHostAgentWorkflowTest()
        {
            var gameEntry = new GameEntry();
            var gameProtocol = PinionCore.Remote.Tools.Protocol.Sources.TestCommon.ProtocolProvider.CreateCase1();
            // 建立遊戲服務
            var gameService = new PinionCore.Remote.Gateway.Servers.Service(gameEntry , gameProtocol);
            // 建立host 監聽的服務 agent
            var userAgent = PinionCore.Remote.Gateway.Provider.CreateAgent();
            var userAgentDisconnect = userAgent.Connect(gameService);
            var userAgentWorker = new AgentWorker(userAgent);
            userAgentWorker.Start();

            // 取得遊戲服務
            var gameObs = from _game in userAgent.QueryNotifier<IGameService>().SupplyEvent()
                          select _game;
            var game = await gameObs.FirstAsync();

            // 建立 gateway host
            var gatewayHost = new PinionCore.Remote.Gateway.Hosts.GatewayHost();

            // 1. 註冊遊戲服務
            gatewayHost.Registry.Register(1, game);

            // 建立 gateway agent
            var gatewayAgent = new PinionCore.Remote.Gateway.Hosts.GatewayAgent(gameProtocol);
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
            userAgentDisconnect();
            gameService.Dispose();
        }
     }
}
