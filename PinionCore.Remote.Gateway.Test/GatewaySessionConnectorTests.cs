using NUnit.Framework;

namespace PinionCore.Remote.Gateway.Tests
{
    public class GatewaySessionConnectorTests
    {
        public GatewaySessionConnectorTests()
        {
        }
        [Test, Timeout(10000)]
        public void Join_SendsJoinPackage()
        {
            var serializer = new PinionCore.Remote.Gateway.Sessions.Serializer(PinionCore.Memorys.PoolProvider.Shared);
            var stream = new PinionCore.Network.Stream();
            var reverseStream = new PinionCore.Network.ReverseStream(stream);

            var serverReader = new PinionCore.Network.PackageReader(stream, PinionCore.Memorys.PoolProvider.Shared);

            var clientReader = new PinionCore.Network.PackageReader(reverseStream, PinionCore.Memorys.PoolProvider.Shared);
            var clientSender = new PinionCore.Network.PackageSender(reverseStream, PinionCore.Memorys.PoolProvider.Shared);
            var connector = new PinionCore.Remote.Gateway.Sessions.GatewaySessionConnector(clientReader, clientSender, serializer);

            PinionCore.Network.IStreamable app = new PinionCore.Network.BufferRelay();
            var id = connector.Join(app);

            var readAwaiter = serverReader.Read().GetAwaiter();
            while (!readAwaiter.IsCompleted)
                connector.HandlePackages();

            var bufs = readAwaiter.GetResult();
            Assert.GreaterOrEqual(bufs.Count, 1);

            var ser = new PinionCore.Remote.Gateway.Sessions.Serializer(PinionCore.Memorys.PoolProvider.Shared);
            bool foundJoin = false;
            foreach (var b in bufs)
            {
                var obj = ser.Deserialize(b);
                var pkg = (PinionCore.Remote.Gateway.Sessions.ClientToServerPackage)obj;
                if (pkg.OpCode == PinionCore.Remote.Gateway.Sessions.OpCodeClientToServer.Join && pkg.Id == id)
                {
                    foundJoin = true;
                    break;
                }
            }
            Assert.IsTrue(foundJoin);
        }

        [Test, Timeout(10000)]
        public void Leave_SendsLeavePackage()
        {
            var serializer = new PinionCore.Remote.Gateway.Sessions.Serializer(PinionCore.Memorys.PoolProvider.Shared);
            var stream = new PinionCore.Network.Stream();
            var reverseStream = new PinionCore.Network.ReverseStream(stream);

            var serverReader = new PinionCore.Network.PackageReader(stream, PinionCore.Memorys.PoolProvider.Shared);

            var clientReader = new PinionCore.Network.PackageReader(reverseStream, PinionCore.Memorys.PoolProvider.Shared);
            var clientSender = new PinionCore.Network.PackageSender(reverseStream, PinionCore.Memorys.PoolProvider.Shared);
            var connector = new PinionCore.Remote.Gateway.Sessions.GatewaySessionConnector(clientReader, clientSender, serializer);

            PinionCore.Network.IStreamable app = new PinionCore.Network.BufferRelay();
            var id = connector.Join(app);

            // Drain join first
            var drainAwait = serverReader.Read().GetAwaiter();
            while (!drainAwait.IsCompleted)
                connector.HandlePackages();

            connector.Leave(app);

            var readAwaiter = serverReader.Read().GetAwaiter();
            while (!readAwaiter.IsCompleted)
                connector.HandlePackages();

            var bufs = readAwaiter.GetResult();
            Assert.GreaterOrEqual(bufs.Count, 1);

            var ser = new PinionCore.Remote.Gateway.Sessions.Serializer(PinionCore.Memorys.PoolProvider.Shared);
            bool foundLeave = false;
            foreach (var b in bufs)
            {
                var obj = ser.Deserialize(b);
                var pkg = (PinionCore.Remote.Gateway.Sessions.ClientToServerPackage)obj;
                if (pkg.OpCode == PinionCore.Remote.Gateway.Sessions.OpCodeClientToServer.Leave && pkg.Id == id)
                {
                    foundLeave = true;
                    break;
                }
            }
            Assert.IsTrue(foundLeave);
        }
    }
}
