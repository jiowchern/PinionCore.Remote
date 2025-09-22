using System;
using System.Linq;
using System.Reactive.Linq;
using NUnit.Framework;
using PinionCore.Network;
using PinionCore.Remote.Gateway.Servers;
using PinionCore.Remote.Gateway.Protocols;
using PinionCore.Remote.Ghost;
using PinionCore.Remote.Reactive;
using PinionCore.Remote.Soul;
using PinionCore.Remote.Standalone;
using PinionCore.Remote.Tools.Protocol.Sources.TestCommon;


namespace PinionCore.Remote.Gateway.Tests
{
    public class ServersTests
    {
        [NUnit.Framework.Test,Timeout(10000)]
        public async System.Threading.Tasks.Task UserGameProtocolIntegrationTest()
        {
            var pool = PinionCore.Memorys.PoolProvider.Shared;

            var gameProtocol = PinionCore.Remote.Tools.Protocol.Sources.TestCommon.ProtocolProvider.CreateCase1();
            var gameEntry = new GameEntry();
            var service = new PinionCore.Remote.Gateway.Servers.GatewayService(gameEntry, gameProtocol);
            var userAgent = Provider.CreateAgent();
            var userAgentDisconnect = userAgent.Connect(service);
            var userUpdateTaskEnable = true;
            var userUpdateTask = System.Threading.Tasks.Task.Run( ()=> {
                while (userUpdateTaskEnable)
                {
                    System.Threading.Thread.Sleep(1);
                    
                    userAgent.HandleMessage();
                    userAgent.HandlePackets();
                }                
            });
            var userObs = from gpi in userAgent.QueryNotifier<IGameLobby>().SupplyEvent()
                          from joinId in gpi.Join().RemoteValue()
                          from user_ in gpi.ClientNotifier.Base.SupplyEvent()
                          where user_.Id == joinId
                          select user_;
            var user = await userObs.FirstAsync();

            var gameAgent = new PinionCore.Remote.Ghost.Agent(gameProtocol, new Remote.Serializer(gameProtocol.SerializeTypes), new PinionCore.Remote.InternalSerializer(), pool) as IAgent;
            gameAgent.Enable(_ToStream(user));
            var gameUpdateTaskEnable = true;
            var gameUpdateTask = System.Threading.Tasks.Task.Run(() => {
                while (gameUpdateTaskEnable)
                {
                    System.Threading.Thread.Sleep(1);
                    gameAgent.HandleMessage();
                    gameAgent.HandlePackets();
                }
            });

            var gameObs = from gpi in gameAgent.QueryNotifier<IMethodable1>().SupplyEvent()
                          from v1 in gpi.GetValue1().RemoteValue()                          
                          select v1;

            var gameGetValue = await gameObs.FirstAsync();

            userUpdateTaskEnable = false;
            await userUpdateTask;

            gameUpdateTaskEnable = false;
            await gameUpdateTask;
            userAgentDisconnect();
            gameAgent.Disable();
            service.Dispose();


            Assert.AreEqual(1, gameGetValue);
        }

        private IStreamable _ToStream(IClientConnection user)
        {
            if (user is IStreamable directStream)
            {
                throw new Exception("");
            }

            return new ClientStreamAdapter(user);
        }
    }
}
