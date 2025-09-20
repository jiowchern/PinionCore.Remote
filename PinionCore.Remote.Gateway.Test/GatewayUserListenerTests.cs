using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using NUnit;
using NUnit.Framework;
using NUnit.Framework.Internal.Execution;
using PinionCore.Network;
using PinionCore.Remote.Gateway.GatewayUserListeners;
using PinionCore.Remote.Gateway.Protocols;
using PinionCore.Remote.Ghost;
using PinionCore.Remote.Reactive;
using PinionCore.Remote.Soul;
using PinionCore.Remote.Tools.Protocol.Sources.TestCommon;


namespace PinionCore.Remote.Gateway.Tests
{
    public class GatewayUserListenerTests
    {
        [NUnit.Framework.Test,Timeout(10000)]
        public async System.Threading.Tasks.Task Test()
        {
            NUnit.Framework.TestContext.Progress.WriteLine("GatewayUserListenerTests.Test start");
            var pool = PinionCore.Memorys.PoolProvider.Shared;
            var gameEntry = new GameEntry();
            var listener = new PinionCore.Remote.Gateway.GatewayUserListeners.GatewayUserListener();
            var userEntry = new PinionCore.Remote.Gateway.GatewayUserListeners.Entry(listener);
            var gameProtocol = PinionCore.Remote.Tools.Protocol.Sources.TestCommon.ProtocolProvider.CreateCase1();
            var userProtocol = PinionCore.Remote.Gateway.Protocols.ProtocolProvider.Create();
            

            var userService = PinionCore.Remote.Standalone.Provider.CreateService(userEntry, userProtocol);
            var gameService = PinionCore.Remote.Gateway.GatewayUserListeners.GameService.Create(gameEntry , gameProtocol, listener);

            var userAgent = userService.Create();
            var userUpdateTaskEnable = true;
            var userUpdateTask = System.Threading.Tasks.Task.Run( ()=> {
                while (userUpdateTaskEnable)
                {
                    System.Threading.Thread.Sleep(1);
                    
                    userAgent.HandleMessage();
                    userAgent.HandlePackets();
                }                
            });
            var userObs = from gpi in userAgent.QueryNotifier<IGatewayUserListener>().SupplyEvent()
                          from joinId in gpi.Join().RemoteValue()
                          from user_ in gpi.UserNotifier.Base.SupplyEvent()
                          where user_.Id == joinId
                          select user_;
            var users = new System.Collections.Generic.List<IUser>();
            var logPath = Path.Combine(AppContext.BaseDirectory, "before_user.log");
            System.IO.File.WriteAllText(logPath, "waiting");
            var user = await userObs.FirstAsync();
            System.IO.File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "user_type.log"), user.GetType().AssemblyQualifiedName ?? "null");

            var gameAgent = new PinionCore.Remote.Ghost.Agent(gameProtocol, new Remote.Serializer(gameProtocol.SerializeTypes), new PinionCore.Remote.InternalSerializer(), pool) as IAgent;
            gameAgent.Enable(_ToStream(user));
            gameAgent.HandleMessage();
            gameAgent.HandlePackets();
            gameAgent.ExceptionEvent += ex =>
            {
                System.IO.File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "game_exception.log"), ex.ToString());
            };
           
            var gameUpdateTaskEnable = true;
            var gameUpdateTask = System.Threading.Tasks.Task.Run(() => {
                while (gameUpdateTaskEnable)
                {
                    System.Threading.Thread.Sleep(1);

                    File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "game_loop.log"), "tick\n");
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

            gameAgent.Disable();
            gameService.Dispose();
            userService.Destroy(userAgent);
            userService.Dispose();


            Assert.AreEqual(1, gameGetValue);
        }

        private IStreamable _ToStream(IUser user)
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "to_stream.log");
            var userId = user.Id.Value;
            var hasBridge = UserStreamRegistry.TryGet(userId, out _);
            File.AppendAllText(logPath, $"user id={userId} streamable={(user is IStreamable)} bridge={hasBridge}\n");
            if (user is IStreamable directStream)
            {
                return directStream;
            }

            return new UserStreamAdapter(user);
        }
    }
}
