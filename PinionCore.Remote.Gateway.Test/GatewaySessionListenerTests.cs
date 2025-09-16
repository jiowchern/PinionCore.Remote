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

            var serializer = new PinionCore.Remote.Gateway.Services.Serializer(PinionCore.Memorys.PoolProvider.Shared);

            // Join to create a session
            const uint id = 7;
            PushPkg(reverseSender, serializer, new PinionCore.Remote.Gateway.Services.ClientToServerPackage
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
            PushPkg(reverseSender, serializer, new PinionCore.Remote.Gateway.Services.ClientToServerPackage
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
        }
    }
}

