
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;



using NUnit.Framework;

using PinionCore.Remote.Gateway.Hosts;
using PinionCore.Remote.Gateway.Protocols;
using PinionCore.Remote.Reactive;
using PinionCore.Remote.Standalone;
using PinionCore.Remote.Tools.Protocol.Sources.TestCommon;
using PinionCore.Remote;



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
        public void RoundRobinStrategyDistributesSessions()
        {
            var strategy = new RoundRobinGameLobbySelectionStrategy();
            var coordinator = new GatewayHostSessionCoordinator(strategy);

            var lobbyA = new TestGameLobby("A");
            var lobbyB = new TestGameLobby("B");

            coordinator.Register(1, lobbyA);
            coordinator.Register(1, lobbyB);

            var session1 = new TestSession();
            coordinator.Join(session1);
            Assert.IsTrue(session1.WaitForBindingCount(1, TimeSpan.FromSeconds(1)));
            Assert.AreSame(lobbyA, session1.GetBoundConnection(1)?.Owner);

            var session2 = new TestSession();
            coordinator.Join(session2);
            Assert.IsTrue(session2.WaitForBindingCount(1, TimeSpan.FromSeconds(1)));
            Assert.AreSame(lobbyB, session2.GetBoundConnection(1)?.Owner);

            var session3 = new TestSession();
            coordinator.Join(session3);
            Assert.IsTrue(session3.WaitForBindingCount(1, TimeSpan.FromSeconds(1)));
            Assert.AreSame(lobbyA, session3.GetBoundConnection(1)?.Owner);

            coordinator.Leave(session1);
            coordinator.Leave(session2);
            coordinator.Leave(session3);
            coordinator.Unregister(lobbyA);
            coordinator.Unregister(lobbyB);
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

        private sealed class TestGameLobby : IGameLobby
        {
            private readonly Dictionary<uint, TestClientConnection> _connections;
            private readonly NotifiableCollection<IClientConnection> _collection;

            public TestGameLobby(string name)
            {
                Name = name;
                _connections = new Dictionary<uint, TestClientConnection>();
                _collection = new NotifiableCollection<IClientConnection>();
                ClientNotifier = new Notifier<IClientConnection>(_collection, _collection);
            }

            public string Name { get; }

            public Notifier<IClientConnection> ClientNotifier { get; }

            private uint _nextUserId;

            public Value<uint> Join()
            {
                var userId = ++_nextUserId;
                var connection = new TestClientConnection(userId, this);
                _connections[userId] = connection;
                ClientNotifier.Collection.Add(connection);

                var value = new Value<uint>();
                value.SetValue(userId);
                return value;
            }

            public Value<ResponseStatus> Leave(uint clientId)
            {
                if (_connections.TryGetValue(clientId, out var connection))
                {
                    _connections.Remove(clientId);
                    ClientNotifier.Collection.Remove(connection);
                }

                return new Value<ResponseStatus>(ResponseStatus.Success);
            }
        }

        private sealed class TestClientConnection : IClientConnection
        {
            public TestClientConnection(uint id, TestGameLobby owner)
            {
                Id = new Property<uint>(id);
                Owner = owner;
            }

            public TestGameLobby Owner { get; }

            public Property<uint> Id { get; }

            public event Action<byte[]> ResponseEvent
            {
                add { }
                remove { }
            }

            public void Request(byte[] payload)
            {
            }
        }

        private sealed class TestSession : IRoutableSession
        {
            private readonly Dictionary<uint, TestClientConnection> _bindings;
            private readonly object _syncRoot;

            public TestSession()
            {
                _bindings = new Dictionary<uint, TestClientConnection>();
                _syncRoot = new object();
            }

            public bool Set(uint group, IClientConnection clientConnection)
            {
                if (clientConnection is not TestClientConnection connection)
                {
                    return false;
                }

                lock (_syncRoot)
                {
                    _bindings[group] = connection;
                    Monitor.PulseAll(_syncRoot);
                }

                return true;
            }

            public bool Unset(uint group)
            {
                lock (_syncRoot)
                {
                    return _bindings.Remove(group);
                }
            }

            public bool WaitForBindingCount(int expectedCount, TimeSpan timeout)
            {
                var deadline = DateTime.UtcNow + timeout;
                lock (_syncRoot)
                {
                    while (_bindings.Count < expectedCount)
                    {
                        var remaining = deadline - DateTime.UtcNow;
                        if (remaining <= TimeSpan.Zero)
                        {
                            return false;
                        }

                        Monitor.Wait(_syncRoot, remaining);
                    }

                    return true;
                }
            }

            public TestClientConnection GetBoundConnection(uint group)
            {
                lock (_syncRoot)
                {
                    return _bindings.TryGetValue(group, out var connection) ? connection : null;
                }
            }
        }
     }
}



