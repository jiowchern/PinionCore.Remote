using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;
using NUnit.Framework;
using PinionCore.Remote.Client;
using PinionCore.Remote.Server;
using PinionCore.Remote.Reactive;

namespace PinionCore.Integration.Tests
{
    public class SampleTests
    {
        [Test,Timeout(5000)]
        public async Task TcpSampleTest()
        {
            var echo = new PinionCore.Remote.Tools.Protocol.Sources.TestCommon.EchoTester(1);
            var entry = NSubstitute.Substitute.For<PinionCore.Remote.IEntry>();
            entry.OnSessionOpened(CreateEchoBinder(echo));

            var protocol = PinionCore.Remote.Tools.Protocol.Sources.TestCommon.ProtocolProvider.CreateCase1();

            var port = PinionCore.Network.Tcp.Tools.GetAvailablePort();
            var host = new PinionCore.Remote.Server.Soul(entry, protocol);

            var disposeServer = await host.ListenAsync(new PinionCore.Remote.Server.Tcp.ListeningEndpoint(port, 10));

            var ghost = new PinionCore.Remote.Client.Ghost(protocol);

            var connector = new PinionCore.Remote.Client.Tcp.ConnectingEndpoint(new IPEndPoint(IPAddress.Loopback, port));
            connector.BreakEvent += () =>
            {
                Assert.Fail("Connection broken unexpectedly.");
            };

            CancellationTokenSource? cts = null;
            Task? runTask = null;

            try
            {
                var disposeClient = await ghost.Connect(connector);

                cts = new CancellationTokenSource();
                runTask = Task.Run(CreateGhostProcessingLoop(ghost, cts), cts.Token);

                var echoObs = from e in ghost.User.QueryNotifier<PinionCore.Remote.Tools.Protocol.Sources.TestCommon.Echoable>().SupplyEvent()
                              from val in e.Echo().RemoteValue()
                              select val;

                var echoValue = await echoObs.FirstAsync();
                Assert.AreEqual(1, echoValue);
                
                cts.Cancel();
                await runTask;

                disposeClient.Dispose();
            }
            catch
            {
                throw;
            }


            disposeServer.Dispose();
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
                        ghost.User.HandleMessage();
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
