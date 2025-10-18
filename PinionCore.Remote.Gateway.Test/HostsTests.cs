
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
        public async System.Threading.Tasks.Task GatewayHostServiceHubAgentWorkflowTest()
        {
            var gameEntry = new TestGameEntry(TestGameEntry.GameType.Method1);
            var gameProtocol = PinionCore.Remote.Tools.Protocol.Sources.TestCommon.ProtocolProvider.CreateCase1();

            // Create the actual game service host
            PinionCore.Remote.Soul.IService actualGameService = Standalone.Provider.CreateService(gameEntry, gameProtocol);

            // Create the gateway-facing service hub
            var connectionService = new PinionCore.Remote.Gateway.Servers.ServiceHub();

            // Bridge Join/Leave events to the actual game service
            connectionService.Sink.StreamableEnterEvent += streamable => actualGameService.Join(streamable);
            connectionService.Sink.StreamableLeaveEvent += streamable => actualGameService.Leave(streamable);

            // Connect host agent to the gateway hub
            var userAgent = Protocols.Provider.CreateAgent();
            var userAgentDisconnect = userAgent.Connect(connectionService.Source);
            var userAgentWorker = new AgentWorker(userAgent);
            userAgentWorker.Start();

            // Retrieve the game lobby from the user agent
            var gameObs = from _game in userAgent.QueryNotifier<IConnectionProvider>().SupplyEvent()
                          select _game;
            var game = await gameObs.FirstAsync();

            // Create the gateway host hub
            var gatewayHost = new PinionCore.Remote.Gateway.Hosts.GatewayHostServiceHub(new RoundRobinGameLobbySelectionStrategy());

            // Register the game lobby with the gateway host            
            gatewayHost.Sink.Register(1, game);

            // Register the game lobby with the gateway host
            var gatewayAgent = new PinionCore.Remote.Gateway.Hosts.AgentPool(gameProtocol);
            // Connect the gateway agent pool to the host
            gatewayAgent.Agent.Connect(gatewayHost.Source);
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
            gatewayAgent.Dispose();

            await gameAgentWorker.StopAsync();
            await gatewayAgentWorker.StopAsync();

            gatewayHost.Sink.Unregister(1, game);
            gatewayHost.Source.Dispose();
            await userAgentWorker.StopAsync();

            // Connect the gateway agent pool to the host
            connectionService.Sink.StreamableEnterEvent -= streamable => actualGameService.Join(streamable);
            connectionService.Sink.StreamableLeaveEvent -= streamable => actualGameService.Leave(streamable);

            userAgentDisconnect();
            connectionService.Dispose();
            actualGameService.Dispose();
        }
     }
}



