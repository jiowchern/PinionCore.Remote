
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
            var listener1 = new PinionCore.Remote.Gateway.Servers.GatewayServerConnectionManager();
            PinionCore.Remote.Gateway.Protocols.IGameLobby service1 = listener1;
            

            var listener2 = new PinionCore.Remote.Gateway.Servers.GatewayServerConnectionManager();
            PinionCore.Remote.Gateway.Protocols.IGameLobby service2 = listener2;

            var user1 = new PinionCore.Remote.Gateway.Hosts.GatewayHostConnectionManager();
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
            var route = new PinionCore.Remote.Gateway.Hosts.GatewayHostSessionCoordinator();
            var user1 = new PinionCore.Remote.Gateway.Hosts.GatewayHostConnectionManager();
            IConnectionManager owner1 = user1;

            var listener1 = new PinionCore.Remote.Gateway.Servers.GatewayServerConnectionManager();
            PinionCore.Remote.Gateway.Protocols.IGameLobby service1 = listener1;

            var listener2 = new PinionCore.Remote.Gateway.Servers.GatewayServerConnectionManager();
            PinionCore.Remote.Gateway.Protocols.IGameLobby service2 = listener2;
            var listener3 = new PinionCore.Remote.Gateway.Servers.GatewayServerConnectionManager();
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

            var user2 = new PinionCore.Remote.Gateway.Hosts.GatewayHostConnectionManager();
            IConnectionManager owner2 = user2;
            route.Join(user2);

            Assert.AreEqual(2, owner2.Connections.Collection.Count);
        }

        [NUnit.Framework.Test, Timeout(10000)]
        public void RoundRobinDistributionTest()
        {
            var coordinator = new PinionCore.Remote.Gateway.Hosts.GatewayHostSessionCoordinator();

            PinionCore.Remote.Gateway.Protocols.IGameLobby lobby1 = new PinionCore.Remote.Gateway.Servers.GatewayServerConnectionManager();
            PinionCore.Remote.Gateway.Protocols.IGameLobby lobby2 = new PinionCore.Remote.Gateway.Servers.GatewayServerConnectionManager();

            coordinator.Register(1, lobby1);
            coordinator.Register(1, lobby2);

            var session1 = new PinionCore.Remote.Gateway.Hosts.GatewayHostConnectionManager();
            var session2 = new PinionCore.Remote.Gateway.Hosts.GatewayHostConnectionManager();
            var session3 = new PinionCore.Remote.Gateway.Hosts.GatewayHostConnectionManager();
            var session4 = new PinionCore.Remote.Gateway.Hosts.GatewayHostConnectionManager();

            coordinator.Join(session1);
            WaitFor(() => ((IConnectionManager)session1).Connections.Collection.Count == 1);

            coordinator.Join(session2);
            WaitFor(() => ((IConnectionManager)session2).Connections.Collection.Count == 1);

            coordinator.Join(session3);
            WaitFor(() => ((IConnectionManager)session3).Connections.Collection.Count == 1);

            WaitFor(() => lobby1.ClientNotifier.Collection.Count + lobby2.ClientNotifier.Collection.Count == 3);

            Assert.AreEqual(2, lobby1.ClientNotifier.Collection.Count);
            Assert.AreEqual(1, lobby2.ClientNotifier.Collection.Count);

            coordinator.Join(session4);
            WaitFor(() => ((IConnectionManager)session4).Connections.Collection.Count == 1);

            WaitFor(() => lobby1.ClientNotifier.Collection.Count + lobby2.ClientNotifier.Collection.Count == 4);

            Assert.AreEqual(2, lobby1.ClientNotifier.Collection.Count);
            Assert.AreEqual(2, lobby2.ClientNotifier.Collection.Count);
        }

        private static void WaitFor(Func<bool> condition)
        {
            if (!SpinWait.SpinUntil(condition, TimeSpan.FromSeconds(1)))
            {
                Assert.Fail("The expected condition was not met within the allotted time.");
            }
        }

        [NUnit.Framework.Test, Timeout(10000)]
        public async System.Threading.Tasks.Task GatewayHostServiceHubAgentWorkflowTest()
        {
            var gameEntry = new GameEntry();
            var gameProtocol = PinionCore.Remote.Tools.Protocol.Sources.TestCommon.ProtocolProvider.CreateCase1();

            // Create the actual game service host
            PinionCore.Remote.Soul.IService actualGameService = Standalone.Provider.CreateService(gameEntry, gameProtocol);

            // Create the gateway-facing service hub
            var connectionService = new PinionCore.Remote.Gateway.Servers.GatewayServerServiceHub();

            // Bridge Join/Leave events to the actual game service
            connectionService.Listener.StreamableEnterEvent += streamable => actualGameService.Join(streamable);
            connectionService.Listener.StreamableLeaveEvent += streamable => actualGameService.Leave(streamable);

            // Connect host agent to the gateway hub
            var userAgent = PinionCore.Remote.Gateway.Provider.CreateAgent();
            var userAgentDisconnect = userAgent.Connect(connectionService.Service);
            var userAgentWorker = new AgentWorker(userAgent);
            userAgentWorker.Start();

            // Retrieve the game lobby from the user agent
            var gameObs = from _game in userAgent.QueryNotifier<IGameLobby>().SupplyEvent()
                          select _game;
            var game = await gameObs.FirstAsync();

            // Create the gateway host hub
            var gatewayHost = new PinionCore.Remote.Gateway.Hosts.GatewayHostServiceHub();

            // Register the game lobby with the gateway host
            gatewayHost.Registry.Register(1, game);

            // Register the game lobby with the gateway host
            var gatewayAgent = new PinionCore.Remote.Gateway.Hosts.GatewayHostClientAgentPool(gameProtocol);
            // Connect the gateway agent pool to the host
            gatewayAgent.Agent.Connect(gatewayHost.Service);
            var gatewayAgentWorker = new AgentWorker(gatewayAgent.Agent);
            gatewayAgentWorker.Start();

            // Create the gateway agent pool
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

            // Connect the gateway agent pool to the host
            connectionService.Listener.StreamableEnterEvent -= streamable => actualGameService.Join(streamable);
            connectionService.Listener.StreamableLeaveEvent -= streamable => actualGameService.Leave(streamable);

            userAgentDisconnect();
            connectionService.Dispose();
            actualGameService.Dispose();
        }
     }
}



