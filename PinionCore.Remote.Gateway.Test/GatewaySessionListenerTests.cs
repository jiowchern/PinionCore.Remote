using System.ComponentModel.DataAnnotations;
using NUnit.Framework;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway.Tests
{
    public class GatewaySessionListenerTests
    {
        private static void PushPkg(PinionCore.Network.PackageSender sender, PinionCore.Remote.Gateway.Services.Serializer serializer, PinionCore.Remote.Gateway.Services.ClientToServerPackage pkg)
        {
            var buf = serializer.Serialize(pkg);
            sender.Push(buf);
        }

        [Test, Timeout(10000)]
        public void EnterEventTriggeredWhenClientJoins()
        {
            var stream = new PinionCore.Network.Stream();
            var reverseStream = new PinionCore.Network.ReverseStream(stream);
            var reverseSender = new PinionCore.Network.PackageSender(reverseStream, PinionCore.Memorys.PoolProvider.Shared);

            var reader = new PinionCore.Network.PackageReader(stream, PinionCore.Memorys.PoolProvider.Shared);
            var sender = new PinionCore.Network.PackageSender(stream, PinionCore.Memorys.PoolProvider.Shared);
            var sessionListener = new PinionCore.Remote.Gateway.Services.GatewaySessionListener(reader, sender);
            var listener = sessionListener as IListenable;

            bool enterFired = false;
            listener.StreamableEnterEvent += (s) => enterFired = true;

            var serializer = new PinionCore.Remote.Gateway.Services.Serializer(PinionCore.Memorys.PoolProvider.Shared);
            PushPkg(reverseSender, serializer, new PinionCore.Remote.Gateway.Services.ClientToServerPackage
            {
                OpCode = PinionCore.Remote.Gateway.Services.OpCodeClientToServer.Join,
                Id = 1,
                Payload = new byte[0]
            });

            while (!enterFired)
                sessionListener.HandlePackages();

            Assert.IsTrue(enterFired);
        }

        [Test, Timeout(10000)]
        public void LeaveEventTriggeredWhenClientLeaves()
        {
            var stream = new PinionCore.Network.Stream();
            var reverseStream = new PinionCore.Network.ReverseStream(stream);
            var reverseSender = new PinionCore.Network.PackageSender(reverseStream, PinionCore.Memorys.PoolProvider.Shared);

            var reader = new PinionCore.Network.PackageReader(stream, PinionCore.Memorys.PoolProvider.Shared);
            var sender = new PinionCore.Network.PackageSender(stream, PinionCore.Memorys.PoolProvider.Shared);
            var sessionListener = new PinionCore.Remote.Gateway.Services.GatewaySessionListener(reader, sender);
            var listener = sessionListener as IListenable;

            bool enterFired = false;
            bool leaveFired = false;
            listener.StreamableEnterEvent += (s) => enterFired = true;
            listener.StreamableLeaveEvent += (s) => leaveFired = true;

            var serializer = new PinionCore.Remote.Gateway.Services.Serializer(PinionCore.Memorys.PoolProvider.Shared);

            // Join first
            PushPkg(reverseSender, serializer, new PinionCore.Remote.Gateway.Services.ClientToServerPackage
            {
                OpCode = PinionCore.Remote.Gateway.Services.OpCodeClientToServer.Join,
                Id = 42,
                Payload = new byte[0]
            });
            while (!enterFired)
                sessionListener.HandlePackages();

            // Then leave
            PushPkg(reverseSender, serializer, new PinionCore.Remote.Gateway.Services.ClientToServerPackage
            {
                OpCode = PinionCore.Remote.Gateway.Services.OpCodeClientToServer.Leave,
                Id = 42,
                Payload = new byte[0]
            });

            while (!leaveFired)
                sessionListener.HandlePackages();

            Assert.IsTrue(leaveFired);
        }

        [Test, Timeout(10000)]
        public void MessageDeliveredToSessionStream()
        {
            var stream = new PinionCore.Network.Stream();
            var reverseStream = new PinionCore.Network.ReverseStream(stream);
            var reverseSender = new PinionCore.Network.PackageSender(reverseStream, PinionCore.Memorys.PoolProvider.Shared);

            var reader = new PinionCore.Network.PackageReader(stream, PinionCore.Memorys.PoolProvider.Shared);
            var sender = new PinionCore.Network.PackageSender(stream, PinionCore.Memorys.PoolProvider.Shared);
            var sessionListener = new PinionCore.Remote.Gateway.Services.GatewaySessionListener(reader, sender);
            var listener = sessionListener as IListenable;

            PinionCore.Network.IStreamable session = null;
            listener.StreamableEnterEvent += (s) => session = s;

            var gateSer = new PinionCore.Remote.Gateway.Services.Serializer(PinionCore.Memorys.PoolProvider.Shared);

            // Join to create a session
            const uint id = 7;
            PushPkg(reverseSender, gateSer, new PinionCore.Remote.Gateway.Services.ClientToServerPackage
            {
                OpCode = PinionCore.Remote.Gateway.Services.OpCodeClientToServer.Join,
                Id = id,
                Payload = new byte[0]
            });
            while (session == null)
                sessionListener.HandlePackages();

            // Prepare to receive
            var payload = new byte[] { 10, 20, 30, 40 };
            var receiveBuffer = new byte[payload.Length];
            var awaiter = session.Receive(receiveBuffer, 0, receiveBuffer.Length).GetAwaiter();

            // Send message payload
            PushPkg(reverseSender, gateSer, new PinionCore.Remote.Gateway.Services.ClientToServerPackage
            {
                OpCode = PinionCore.Remote.Gateway.Services.OpCodeClientToServer.Message,
                Id = id,
                Payload = payload
            });

            // Pump until receive completed
            while (!awaiter.IsCompleted)
                sessionListener.HandlePackages();

            var received = awaiter.GetResult();
            Assert.AreEqual(payload.Length, received);
            Assert.AreEqual(payload, receiveBuffer);

            // Verify server-to-client forwarding via reverseReader
            var reverseReader = new PinionCore.Network.PackageReader(reverseStream, PinionCore.Memorys.PoolProvider.Shared);

            // Trigger a send from session to client
            var outgoing = new byte[] { 1, 2, 3 };
            var sendAwaiter = session.Send(outgoing, 0, outgoing.Length).GetAwaiter();

            // Pump listener until reverseReader receives the packet
            var readAwaiter = reverseReader.Read().GetAwaiter();
            while (!readAwaiter.IsCompleted)
                sessionListener.HandlePackages();

            var bufs = readAwaiter.GetResult();
            Assert.GreaterOrEqual(bufs.Count, 1);
            var rawSer = new PinionCore.Serialization.Serializer(new PinionCore.Serialization.DescriberBuilder(new System.Type[]
            {
                typeof(PinionCore.Remote.Gateway.Services.ServerToClientPackage),
                typeof(PinionCore.Remote.Gateway.Services.OpCodeServerToClient),
                typeof(uint),
                typeof(byte[]),
                typeof(byte)
            }).Describers, PinionCore.Memorys.PoolProvider.Shared);
            var pkgObj = ((PinionCore.Remote.Gateway.Serializer)gateSer).Deserialize(bufs[0]);
            var serverPkg = (PinionCore.Remote.Gateway.Services.ServerToClientPackage)pkgObj;
            Assert.AreEqual(PinionCore.Remote.Gateway.Services.OpCodeServerToClient.Message, serverPkg.OpCode);
            Assert.AreEqual(id, serverPkg.Id);
            Assert.AreEqual(outgoing, serverPkg.Payload);
            Assert.IsTrue(sendAwaiter.IsCompleted);
            Assert.AreEqual(outgoing.Length, sendAwaiter.GetResult());
        }

        [Test, Timeout(10000)]
        public void Send_AfterJoin_ForwardedToClient()
        {
            var stream = new PinionCore.Network.Stream();
            var reverseStream = new PinionCore.Network.ReverseStream(stream);
            var reverseSender = new PinionCore.Network.PackageSender(reverseStream, PinionCore.Memorys.PoolProvider.Shared);

            var reader = new PinionCore.Network.PackageReader(stream, PinionCore.Memorys.PoolProvider.Shared);
            var sender = new PinionCore.Network.PackageSender(stream, PinionCore.Memorys.PoolProvider.Shared);
            var sessionListener = new PinionCore.Remote.Gateway.Services.GatewaySessionListener(reader, sender);
            var listener = sessionListener as IListenable;

            PinionCore.Network.IStreamable session = null;
            listener.StreamableEnterEvent += (s) => session = s;

            var gateSer = new PinionCore.Remote.Gateway.Services.Serializer(PinionCore.Memorys.PoolProvider.Shared);

            // Join first to create session
            const uint id = 88;
            PushPkg(reverseSender, gateSer, new PinionCore.Remote.Gateway.Services.ClientToServerPackage
            {
                OpCode = PinionCore.Remote.Gateway.Services.OpCodeClientToServer.Join,
                Id = id,
                Payload = new byte[0]
            });
            while (session == null)
                sessionListener.HandlePackages();

            // Prepare reverseReader to capture server-to-client message
            var reverseReader = new PinionCore.Network.PackageReader(reverseStream, PinionCore.Memorys.PoolProvider.Shared);
            var readAwaiter = reverseReader.Read().GetAwaiter();

            // Send payload from session to client
            var outgoing = new byte[] { 7, 8, 9, 10 };
            var sendAwaiter = session.Send(outgoing, 0, outgoing.Length).GetAwaiter();

            // Pump until server-to-client message arrives
            while (!readAwaiter.IsCompleted)
                sessionListener.HandlePackages();

            var bufs = readAwaiter.GetResult();
            Assert.GreaterOrEqual(bufs.Count, 1);
            var pkgObj = ((PinionCore.Remote.Gateway.Serializer)gateSer).Deserialize(bufs[0]);
            var serverPkg = (PinionCore.Remote.Gateway.Services.ServerToClientPackage)pkgObj;

            Assert.AreEqual(PinionCore.Remote.Gateway.Services.OpCodeServerToClient.Message, serverPkg.OpCode);
            Assert.AreEqual(id, serverPkg.Id);
            Assert.AreEqual(outgoing, serverPkg.Payload);
            Assert.IsTrue(sendAwaiter.IsCompleted);
            Assert.AreEqual(outgoing.Length, sendAwaiter.GetResult());
        }
    }
}

