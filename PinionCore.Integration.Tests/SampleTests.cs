using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;
using NUnit.Framework;
using PinionCore.Remote.Client;
using PinionCore.Remote.Server;
using PinionCore.Remote.Reactive;
using System.Linq;

namespace PinionCore.Integration.Tests
{
    public class SampleTests
    {
        [Test,Timeout(5000)]
        public async Task SampleTest()
        {
            var echo = new PinionCore.Remote.Tools.Protocol.Sources.TestCommon.EchoTester(1);
            var entry = NSubstitute.Substitute.For<PinionCore.Remote.IEntry>();
            entry.OnSessionOpened(CreateEchoBinder(echo));

            var protocol = PinionCore.Remote.Tools.Protocol.Sources.TestCommon.ProtocolProvider.CreateCase1();

            var ports = PinionCore.Network.Tcp.Tools.GetAvailablePorts(2).ToArray();
            var tcpPort = ports[0];
            var webPort = ports[1];
            var soul = new PinionCore.Remote.Server.Soul(entry, protocol);

            var standaloneListener = new PinionCore.Remote.Standalone.ListeningEndpoint();
            var (disposeServer, errorInfos) = await soul.ListenAsync(
                new PinionCore.Remote.Server.Tcp.ListeningEndpoint(tcpPort, 10) ,
                new PinionCore.Remote.Server.Web.ListeningEndpoint($"http://localhost:{webPort}/"),
                standaloneListener
               );

            foreach (var err in errorInfos)
            {
                Assert.Fail($"Server error: {err.Exception}");
            }

            try
            {
                var tcpTask = Task.Run(async () =>
                {
                    var tcpConnector = new PinionCore.Remote.Client.Tcp.ConnectingEndpoint(new IPEndPoint(IPAddress.Loopback, tcpPort));
                    var tcpGhost = new PinionCore.Remote.Client.Ghost(protocol);
                    var tcpDisposeClient = await tcpGhost.Connect(tcpConnector);
                    await RunGhostEchoTestAsync(tcpGhost);
                    tcpDisposeClient.Dispose();
                });
                
                var webTask = Task.Run(async () =>
                {
                    var webConnector = new PinionCore.Remote.Client.Web.ConnectingEndpoint($"ws://localhost:{webPort}/");
                    var webGhost = new PinionCore.Remote.Client.Ghost(protocol);
                    var webDisposeClient = await webGhost.Connect(webConnector);
                    await RunGhostEchoTestAsync(webGhost);
                    webDisposeClient.Dispose();
                });

                var standaloneTask = Task.Run(async () =>
                {
                    var standaloneGhost = new PinionCore.Remote.Client.Ghost(protocol);
                    var standaloneDisposeClient = await standaloneGhost.Connect(standaloneListener);
                    await RunGhostEchoTestAsync(standaloneGhost);
                    standaloneDisposeClient.Dispose();
                });

                Task.WaitAll(tcpTask, webTask, standaloneTask);
            }
            catch
            {
                throw;
            }

            disposeServer.Dispose();
        }

        private static async Task RunGhostEchoTestAsync(Ghost ghost)
        {
            CancellationTokenSource? cts = null;
            Task? runTask = null;
            

            

            cts = new CancellationTokenSource();
            runTask = Task.Run(CreateGhostProcessingLoop(ghost, cts), cts.Token);

            var echoObs = from e in ghost.User.QueryNotifier<PinionCore.Remote.Tools.Protocol.Sources.TestCommon.Echoable>().SupplyEvent()
                          from val in e.Echo().RemoteValue()
                          select val;

            var echoValue = await echoObs.FirstAsync();
            Assert.AreEqual(1, echoValue);

            cts.Cancel();
            await runTask;

            
        }

        private static Remote.ISessionBinder CreateEchoBinder(Remote.Tools.Protocol.Sources.TestCommon.EchoTester echo)
        {
            return NSubstitute.Arg.Do<PinionCore.Remote.ISessionBinder>(b => BindEchoable(echo, b));
        }

        private static Remote.ISoul BindEchoable(Remote.Tools.Protocol.Sources.TestCommon.EchoTester echo, Remote.ISessionBinder b)
        {
            return b.Bind<PinionCore.Remote.Tools.Protocol.Sources.TestCommon.Echoable>(echo);
        }

        private static System.Func<Task> CreateGhostProcessingLoop(Ghost ghost, CancellationTokenSource cts)
        {
            return async () =>
            {
                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        ghost.User.HandlePackets();
                        ghost.User.HandleMessages();
                        await Task.Delay(1, cts.Token);
                    }
                }
                catch (System.OperationCanceledException) when (cts.IsCancellationRequested)
                {
                    // 預期的取消，視為正常結束
                }
            };
        }
    }
}
