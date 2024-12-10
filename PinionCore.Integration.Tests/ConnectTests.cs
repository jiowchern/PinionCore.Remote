using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using PinionCore.Remote;
using PinionCore.Remote.Reactive;
namespace PinionCore.Integration.Tests
{
    public class ConnectTests
    {
        [Test]

        public async Task AgentDisconnectTest()
        {
            var port = PinionCore.Network.Tcp.Tools.GetAvailablePort();
            var tester = new PinionCore.Remote.Tools.Protocol.Sources.TestCommon.MethodTester();
            IEntry entry = NSubstitute.Substitute.For<IEntry>();
            entry.RegisterClientBinder(NSubstitute.Arg.Do<IBinder>(b => b.Bind<PinionCore.Remote.Tools.Protocol.Sources.TestCommon.IMethodable>(tester)));
            IProtocol protocol = PinionCore.Remote.Tools.Protocol.Sources.TestCommon.ProtocolProvider.CreateCase1();

            Remote.Server.TcpListenSet server = PinionCore.Remote.Server.Provider.CreateTcpService(entry, protocol);
            server.Listener.Bind(port);

            Remote.Client.TcpConnectSet client = PinionCore.Remote.Client.Provider.CreateTcpAgent(protocol);
            System.Exception ex;

            client.Agent.ExceptionEvent += (exc) =>
            {
                ex = exc;
            };

            Network.Tcp.Peer peer = await client.Connector.Connect(new IPEndPoint(IPAddress.Loopback, port));
            var peerBreak = false;
            peer.BreakEvent += () =>
            {
                peerBreak = true;
            };




            client.Agent.Enable(peer);

            await client.Connector.Disconnect();

            while (!peerBreak)
            {
                client.Agent.HandleMessage();
                client.Agent.HandlePackets();
            }


        }
        [Test]
        public async Task TcpLocalConnectTest()
        {
            // 獲取一個臨時可用的端口
            int GetAvailablePort()
            {
                var listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                var port = ((IPEndPoint)listener.LocalEndpoint).Port;
                listener.Stop();
                return port;
            }
            var port = GetAvailablePort();
            // bind interface
            var tester = new PinionCore.Remote.Tools.Protocol.Sources.TestCommon.MethodTester();
            IEntry entry = NSubstitute.Substitute.For<IEntry>();
            entry.RegisterClientBinder(NSubstitute.Arg.Do<IBinder>(b => b.Bind<PinionCore.Remote.Tools.Protocol.Sources.TestCommon.IMethodable>(tester)));

            // create protocol
            IProtocol protocol = PinionCore.Remote.Tools.Protocol.Sources.TestCommon.ProtocolProvider.CreateCase1();

            // create server and client
            Remote.Server.TcpListenSet server = PinionCore.Remote.Server.Provider.CreateTcpService(entry, protocol);

            server.Listener.Bind(port);

            Remote.Client.TcpConnectSet client = PinionCore.Remote.Client.Provider.CreateTcpAgent(protocol);

            var stop = false;
            var task = System.Threading.Tasks.Task.Run(() =>
            {
                while (!stop)
                {
                    client.Agent.HandlePackets();
                    client.Agent.HandleMessage();
                }

            });

            // do connect
            System.Net.IPEndPoint endPoint;
            System.Net.IPEndPoint.TryParse($"127.0.0.1:{port}", out endPoint);
            Network.Tcp.Peer peer = await client.Connector.Connect(endPoint);

            client.Agent.Enable(peer);
            // get values
            var valuesObs = from gpi in client.Agent.QueryNotifier<PinionCore.Remote.Tools.Protocol.Sources.TestCommon.IMethodable>().SupplyEvent()
                            from v1 in gpi.GetValue1().RemoteValue()
                            from v2 in gpi.GetValue2().RemoteValue()
                            from v0 in gpi.GetValue0(0, "", 0, 0, 0, System.Guid.Empty).RemoteValue()
                            select new { v1, v2, v0 };


            var values = await valuesObs.FirstAsync();
            stop = true;
            await task;

            // release
            await client.Connector.Disconnect();
            client.Agent.Disable();

            server.Listener.Close();

            server.Service.Dispose();

            // test
            Assert.AreEqual(1, values.v1);
            Assert.AreEqual(2, values.v2);
            Assert.AreEqual(0, values.v0[0]);
        }
    }
}
