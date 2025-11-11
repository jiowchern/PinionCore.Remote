using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;
using PinionCore.Remote.Client;
using PinionCore.Remote.Server;

namespace PinionCore.Integration.Tests
{
    public class SampleTests
    {
        [Test]
        public async Task TcpSampleTest()
        {

            var echo = new PinionCore.Remote.Tools.Protocol.Sources.TestCommon.EchoTester();
            var entry = NSubstitute.Substitute.For<PinionCore.Remote.IEntry>();
            entry.RegisterClientBinder(NSubstitute.Arg.Do<PinionCore.Remote.IBinder>(b => b.Bind<PinionCore.Remote.Tools.Protocol.Sources.TestCommon.Echoable>(echo)));

            var protocol = PinionCore.Remote.Tools.Protocol.Sources.TestCommon.ProtocolProvider.CreateCase1();

            var port = PinionCore.Network.Tcp.Tools.GetAvailablePort();
            var host = new PinionCore.Remote.Server.Host(entry, protocol);

            var disposeServer = await host.ListenAsync(new PinionCore.Remote.Server.Tcp.ListeningEndpoint(port, 10));

            var node = new PinionCore.Remote.Client.Node(protocol);

            var connector = new PinionCore.Remote.Client.Tcp.ConnectingEndpoint(new System.Net.IPEndPoint(IPAddress.Loopback, port));
            connector.BreakEvent += () =>
            {
                Assert.Fail("Connection broken unexpectedly.");
            };
            var disposeClient = await node.Connect(connector);
            




            disposeClient.Dispose();
            disposeServer.Dispose();

        }
    }
}
