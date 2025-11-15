using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using PinionCore.Remote;
using PinionCore.Remote.Client;
using PinionCore.Remote.Server;
using PinionCore.Remote.Reactive;
using System;

namespace PinionCore.Integration.Tests
{
    public class ConnectTests
    {
        [Test,Timeout(10000)]
        public async Task AgentDisconnectTest()
        {
            var port = PinionCore.Network.Tcp.Tools.GetAvailablePort();
            var tester = new PinionCore.Remote.Tools.Protocol.Sources.TestCommon.MethodTester();
            IEntry entry = NSubstitute.Substitute.For<IEntry>();
            entry.OnSessionOpened(NSubstitute.Arg.Do<ISessionBinder>(b => b.Bind<PinionCore.Remote.Tools.Protocol.Sources.TestCommon.IMethodable>(tester)));
            IProtocol protocol = PinionCore.Remote.Tools.Protocol.Sources.TestCommon.ProtocolProvider.CreateCase1();

            var soul = new PinionCore.Remote.Server.Soul(entry, protocol);
            PinionCore.Remote.Soul.IService service = soul;
            var (disposeServer, errorInfos) = await service.ListenAsync(
                new PinionCore.Remote.Server.Tcp.ListeningEndpoint(port, 10));

            foreach (var error in errorInfos)
            {
                Assert.Fail($"Server error: {error.Exception}");
            }

            var ghost = new PinionCore.Remote.Client.Ghost(protocol);
            var endpoint = new PinionCore.Remote.Client.Tcp.ConnectingEndpoint(new IPEndPoint(IPAddress.Loopback, port));
            var connectable = (PinionCore.Remote.Client.IConnectingEndpoint)endpoint;
            var stream = await connectable.ConnectAsync().ConfigureAwait(false);
            var peer = stream as PinionCore.Network.Tcp.Peer ?? throw new InvalidOperationException("Expected TCP peer.");
            var peerBreak = false;
            peer.BreakEvent += () => peerBreak = true;

            ghost.User.Enable(stream);

            await peer.Disconnect().ConfigureAwait(false);

            while (!peerBreak)
            {
                ghost.User.HandleMessages();
                ghost.User.HandlePackets();
            }

            ghost.User.Disable();
            ((IDisposable)endpoint).Dispose();
            disposeServer.Dispose();
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
            entry.OnSessionOpened(NSubstitute.Arg.Do<ISessionBinder>(b => b.Bind<PinionCore.Remote.Tools.Protocol.Sources.TestCommon.IMethodable>(tester)));

            // create protocol
            IProtocol protocol = PinionCore.Remote.Tools.Protocol.Sources.TestCommon.ProtocolProvider.CreateCase1();

            var soul = new PinionCore.Remote.Server.Soul(entry, protocol);
            PinionCore.Remote.Soul.IService service = soul;
            var (disposeServer, errorInfos) = await service.ListenAsync(
                new PinionCore.Remote.Server.Tcp.ListeningEndpoint(port, 10));

            foreach (var error in errorInfos)
            {
                Assert.Fail($"Server error: {error.Exception}");
            }

            var ghost = new PinionCore.Remote.Client.Ghost(protocol);

            var stop = false;
            var task = System.Threading.Tasks.Task.Run(() =>
            {
                while (!stop)
                {
                    ghost.User.HandlePackets();
                    ghost.User.HandleMessages();
                }

            });

            // do connect
            var endpoint = new PinionCore.Remote.Client.Tcp.ConnectingEndpoint(new IPEndPoint(IPAddress.Loopback, port));
            var connectable = (PinionCore.Remote.Client.IConnectingEndpoint)endpoint;
            var stream = await connectable.ConnectAsync().ConfigureAwait(false);

            ghost.User.Enable(stream);
            // get values
            var valuesObs = from gpi in ghost.User.QueryNotifier<PinionCore.Remote.Tools.Protocol.Sources.TestCommon.IMethodable>().SupplyEvent()
                            from v1 in gpi.GetValue1().RemoteValue()
                            from v2 in gpi.GetValue2().RemoteValue()
                            from v0 in gpi.GetValue0(0, "", 0, 0, 0, System.Guid.Empty).RemoteValue()
                            select new { v1, v2, v0 };


            var values = await valuesObs.FirstAsync();
            stop = true;
            await task;

            // release
            ghost.User.Disable();
            ((IDisposable)endpoint).Dispose();
            disposeServer.Dispose();

            // test
            Assert.AreEqual(1, values.v1);
            Assert.AreEqual(2, values.v2);
            Assert.AreEqual(0, values.v0[0]);
        }


        
    }
}
