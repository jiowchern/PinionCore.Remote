using System;
using System.Threading;
using NUnit.Framework;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway.Tests
{
    public class GatewaySessionListenerTests
    {
        private static void PushPkg(PinionCore.Network.PackageSender sender, PinionCore.Remote.Gateway.Backends.Serializer serializer, PinionCore.Remote.Gateway.Backends.ClientToServerPackage pkg)
        {
            var buffer = serializer.Serialize(pkg);
            sender.Push(buffer);
        }

        [Test, Timeout(10000)]
        public void EnterEventTriggeredWhenClientJoins()
        {
            var serializer = new PinionCore.Remote.Gateway.Backends.Serializer(PinionCore.Memorys.PoolProvider.Shared);
            var stream = new PinionCore.Network.Stream();
            var reverseStream = new PinionCore.Network.ReverseStream(stream);
            var reverseSender = new PinionCore.Network.PackageSender(reverseStream, PinionCore.Memorys.PoolProvider.Shared);

            var reader = new PinionCore.Network.PackageReader(stream, PinionCore.Memorys.PoolProvider.Shared);
            var sender = new PinionCore.Network.PackageSender(stream, PinionCore.Memorys.PoolProvider.Shared);
            using var backendServer = new PinionCore.Remote.Gateway.Backends.BackendServer(reader, sender, serializer);
            var listener = (IListenable)backendServer;

            using var enterEvent = new ManualResetEventSlim(false);
            listener.StreamableEnterEvent += _ => enterEvent.Set();

            backendServer.Start();

            PushPkg(reverseSender, serializer, new PinionCore.Remote.Gateway.Backends.ClientToServerPackage
            {
                OpCode = PinionCore.Remote.Gateway.Backends.OpCodeClientToServer.Join,
                Id = 1,
                Payload = Array.Empty<byte>()
            });

            Assert.That(enterEvent.Wait(TimeSpan.FromSeconds(5)), Is.True);
        }

        [Test, Timeout(10000)]
        public void LeaveEventTriggeredWhenClientLeaves()
        {
            var serializer = new PinionCore.Remote.Gateway.Backends.Serializer(PinionCore.Memorys.PoolProvider.Shared);
            var stream = new PinionCore.Network.Stream();
            var reverseStream = new PinionCore.Network.ReverseStream(stream);
            var reverseSender = new PinionCore.Network.PackageSender(reverseStream, PinionCore.Memorys.PoolProvider.Shared);

            var reader = new PinionCore.Network.PackageReader(stream, PinionCore.Memorys.PoolProvider.Shared);
            var sender = new PinionCore.Network.PackageSender(stream, PinionCore.Memorys.PoolProvider.Shared);
            using var backendServer = new PinionCore.Remote.Gateway.Backends.BackendServer(reader, sender, serializer);
            var listener = (IListenable)backendServer;

            using var enterEvent = new ManualResetEventSlim(false);
            using var leaveEvent = new ManualResetEventSlim(false);
            listener.StreamableEnterEvent += _ => enterEvent.Set();
            listener.StreamableLeaveEvent += _ => leaveEvent.Set();

            backendServer.Start();

            PushPkg(reverseSender, serializer, new PinionCore.Remote.Gateway.Backends.ClientToServerPackage
            {
                OpCode = PinionCore.Remote.Gateway.Backends.OpCodeClientToServer.Join,
                Id = 42,
                Payload = Array.Empty<byte>()
            });
            Assert.That(enterEvent.Wait(TimeSpan.FromSeconds(5)), Is.True);

            PushPkg(reverseSender, serializer, new PinionCore.Remote.Gateway.Backends.ClientToServerPackage
            {
                OpCode = PinionCore.Remote.Gateway.Backends.OpCodeClientToServer.Leave,
                Id = 42,
                Payload = Array.Empty<byte>()
            });

            Assert.That(leaveEvent.Wait(TimeSpan.FromSeconds(5)), Is.True);
        }

        [Test, Timeout(10000)]
        public void MessageDeliveredToSessionStream()
        {
            var serializer = new PinionCore.Remote.Gateway.Backends.Serializer(PinionCore.Memorys.PoolProvider.Shared);
            var stream = new PinionCore.Network.Stream();
            var reverseStream = new PinionCore.Network.ReverseStream(stream);
            var reverseSender = new PinionCore.Network.PackageSender(reverseStream, PinionCore.Memorys.PoolProvider.Shared);

            var reader = new PinionCore.Network.PackageReader(stream, PinionCore.Memorys.PoolProvider.Shared);
            var sender = new PinionCore.Network.PackageSender(stream, PinionCore.Memorys.PoolProvider.Shared);
            using var backendServer = new PinionCore.Remote.Gateway.Backends.BackendServer(reader, sender, serializer);
            var listener = (IListenable)backendServer;

            PinionCore.Network.IStreamable session = null;
            using var sessionReady = new ManualResetEventSlim(false);
            listener.StreamableEnterEvent += s =>
            {
                session = s;
                sessionReady.Set();
            };

            backendServer.Start();

            var gateSerializer = new PinionCore.Remote.Gateway.Backends.Serializer(PinionCore.Memorys.PoolProvider.Shared);

            const uint id = 7;
            PushPkg(reverseSender, serializer, new PinionCore.Remote.Gateway.Backends.ClientToServerPackage
            {
                OpCode = PinionCore.Remote.Gateway.Backends.OpCodeClientToServer.Join,
                Id = id,
                Payload = Array.Empty<byte>()
            });
            Assert.That(sessionReady.Wait(TimeSpan.FromSeconds(5)), Is.True);

            var receiveBuffer = new byte[32];
            var awaiter = session.Receive(receiveBuffer, 0, receiveBuffer.Length).GetAwaiter();

            var payload = new byte[] { 10, 11, 12 };
            PushPkg(reverseSender, gateSerializer, new PinionCore.Remote.Gateway.Backends.ClientToServerPackage
            {
                OpCode = PinionCore.Remote.Gateway.Backends.OpCodeClientToServer.Message,
                Id = id,
                Payload = payload
            });

            Assert.That(SpinWait.SpinUntil(() => awaiter.IsCompleted, TimeSpan.FromSeconds(5)), Is.True);

            var received = awaiter.GetResult();
            Assert.AreEqual(payload.Length, received);
            Assert.AreEqual(payload, receiveBuffer[..payload.Length]);

            var reverseReader = new PinionCore.Network.PackageReader(reverseStream, PinionCore.Memorys.PoolProvider.Shared);

            var outgoing = new byte[] { 1, 2, 3 };
            var sendAwaiter = session.Send(outgoing, 0, outgoing.Length).GetAwaiter();

            var readAwaiter = reverseReader.Read().GetAwaiter();
            Assert.That(SpinWait.SpinUntil(() => readAwaiter.IsCompleted, TimeSpan.FromSeconds(5)), Is.True);

            var buffers = readAwaiter.GetResult();
            Assert.GreaterOrEqual(buffers.Count, 1);
            var serializerRaw = new PinionCore.Serialization.Serializer(
                new PinionCore.Serialization.DescriberBuilder(new Type[]
                {
                    typeof(PinionCore.Remote.Gateway.Backends.ServerToClientPackage),
                    typeof(PinionCore.Remote.Gateway.Backends.OpCodeServerToClient),
                    typeof(uint),
                    typeof(byte[]),
                    typeof(byte)
                }).Describers,
                PinionCore.Memorys.PoolProvider.Shared);
            var packageObj = ((PinionCore.Remote.Gateway.Serializer)gateSerializer).Deserialize(buffers[0]);
            var serverPackage = (PinionCore.Remote.Gateway.Backends.ServerToClientPackage)packageObj;

            Assert.AreEqual(PinionCore.Remote.Gateway.Backends.OpCodeServerToClient.Message, serverPackage.OpCode);
            Assert.AreEqual(id, serverPackage.Id);
            Assert.AreEqual(outgoing, serverPackage.Payload);
            Assert.IsTrue(sendAwaiter.IsCompleted);
            Assert.AreEqual(outgoing.Length, sendAwaiter.GetResult());
        }

        [Test, Timeout(10000)]
        public void Send_AfterJoin_ForwardedToClient()
        {
            var serializer = new PinionCore.Remote.Gateway.Backends.Serializer(PinionCore.Memorys.PoolProvider.Shared);
            var stream = new PinionCore.Network.Stream();
            var reverseStream = new PinionCore.Network.ReverseStream(stream);
            var reverseSender = new PinionCore.Network.PackageSender(reverseStream, PinionCore.Memorys.PoolProvider.Shared);

            var reader = new PinionCore.Network.PackageReader(stream, PinionCore.Memorys.PoolProvider.Shared);
            var sender = new PinionCore.Network.PackageSender(stream, PinionCore.Memorys.PoolProvider.Shared);
            using var backendServer = new PinionCore.Remote.Gateway.Backends.BackendServer(reader, sender, serializer);
            var listener = (IListenable)backendServer;

            PinionCore.Network.IStreamable session = null;
            using var sessionReady = new ManualResetEventSlim(false);
            listener.StreamableEnterEvent += s =>
            {
                session = s;
                sessionReady.Set();
            };

            backendServer.Start();

            var gateSerializer = new PinionCore.Remote.Gateway.Backends.Serializer(PinionCore.Memorys.PoolProvider.Shared);

            const uint id = 88;
            PushPkg(reverseSender, gateSerializer, new PinionCore.Remote.Gateway.Backends.ClientToServerPackage
            {
                OpCode = PinionCore.Remote.Gateway.Backends.OpCodeClientToServer.Join,
                Id = id,
                Payload = Array.Empty<byte>()
            });
            Assert.That(sessionReady.Wait(TimeSpan.FromSeconds(5)), Is.True);

            var reverseReader = new PinionCore.Network.PackageReader(reverseStream, PinionCore.Memorys.PoolProvider.Shared);
            var readAwaiter = reverseReader.Read().GetAwaiter();

            var outgoing = new byte[] { 7, 8, 9, 10 };
            var sendAwaiter = session.Send(outgoing, 0, outgoing.Length).GetAwaiter();

            Assert.That(SpinWait.SpinUntil(() => readAwaiter.IsCompleted, TimeSpan.FromSeconds(5)), Is.True);

            var buffers = readAwaiter.GetResult();
            Assert.GreaterOrEqual(buffers.Count, 1);
            var packageObj = ((PinionCore.Remote.Gateway.Serializer)gateSerializer).Deserialize(buffers[0]);
            var serverPackage = (PinionCore.Remote.Gateway.Backends.ServerToClientPackage)packageObj;

            Assert.AreEqual(PinionCore.Remote.Gateway.Backends.OpCodeServerToClient.Message, serverPackage.OpCode);
            Assert.AreEqual(id, serverPackage.Id);
            Assert.AreEqual(outgoing, serverPackage.Payload);
            Assert.IsTrue(sendAwaiter.IsCompleted);
            Assert.AreEqual(outgoing.Length, sendAwaiter.GetResult());
        }
    }
}
