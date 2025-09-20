using System;
using System.Buffers.Binary;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using PinionCore.Memorys;

namespace PinionCore.Remote.Gateway.Tests
{
    public class ServiceRegistryTests
    {
        private const uint DefaultGroup = 1;

        [Test]
        public async Task JoinRoutesMessagesBothWays()
        {
            var pool = PoolProvider.Shared;
            var serializer = new ServiceRegistrySessionListenerSerializer(pool);

            using var registry = new ServiceRegistry(pool, serializer);

            var serviceStream = new PinionCore.Network.Stream();
            var gatewayStream = new PinionCore.Network.ReverseStream(serviceStream);
            registry.Register(DefaultGroup, gatewayStream);

            var serviceReader = new PinionCore.Network.PackageReader(serviceStream, pool);
            var serviceSender = new PinionCore.Network.PackageSender(serviceStream, pool);

            var clientStream = new PinionCore.Network.Stream();
            var gatewayUserStream = new PinionCore.Network.ReverseStream(clientStream);
            var userReader = new PinionCore.Network.PackageReader(clientStream, pool);
            var userSender = new PinionCore.Network.PackageSender(clientStream, pool);

            var session = new UserSession(42, gatewayUserStream, pool);
            registry.Join(session);

            var joinBuffers = await serviceReader.Read();
            Assert.That(joinBuffers.Count, Is.GreaterThan(0));
            var joinPackage = (ServiceRegistryPackage)serializer.Deserialize(joinBuffers[0]);
            Assert.That(joinPackage.OpCode, Is.EqualTo(OpCodeFromServiceRegistry.Join));
            Assert.That(joinPackage.UserId, Is.EqualTo(session.Id));

            var servicePayload = new byte[] { 0, 10, 20, 30 };
            var servicePackage = new SessionListenerPackage
            {
                OpCode = OpCodeFromSessionListener.Message,
                UserId = session.Id,
                Payload = servicePayload
            };
            var serviceBuffer = serializer.Serialize(servicePackage);
            serviceSender.Push(serviceBuffer);

            var userBuffers = await userReader.Read();
            Assert.That(userBuffers.Count, Is.GreaterThan(0));
            var userMessage = ReadMessage(userBuffers[0]);
            Assert.That(userMessage.Group, Is.EqualTo(DefaultGroup));
            CollectionAssert.AreEqual(servicePayload, userMessage.Payload);

            var clientPayload = new byte[] { 5, 4, 3, 2, 1 };
            var clientBuffer = pool.Alloc(clientPayload.Length + sizeof(uint));
            var segment = clientBuffer.Bytes;
            BinaryPrimitives.WriteUInt32LittleEndian(segment.Array.AsSpan(segment.Offset, sizeof(uint)), DefaultGroup);
            Array.Copy(clientPayload, 0, segment.Array, segment.Offset + sizeof(uint), clientPayload.Length);
            userSender.Push(clientBuffer);

            var messageBuffers = await serviceReader.Read();
            Assert.That(messageBuffers.Count, Is.GreaterThan(0));
            var messagePackage = (ServiceRegistryPackage)serializer.Deserialize(messageBuffers[0]);
            Assert.That(messagePackage.OpCode, Is.EqualTo(OpCodeFromServiceRegistry.Message));
            Assert.That(messagePackage.UserId, Is.EqualTo(session.Id));
            CollectionAssert.AreEqual(clientPayload, messagePackage.Payload);

            registry.Leave(session);

            var leaveBuffers = await serviceReader.Read();
            Assert.That(leaveBuffers.Count, Is.GreaterThan(0));
            var leavePackage = (ServiceRegistryPackage)serializer.Deserialize(leaveBuffers[0]);
            Assert.That(leavePackage.OpCode, Is.EqualTo(OpCodeFromServiceRegistry.Leave));
            Assert.That(leavePackage.UserId, Is.EqualTo(session.Id));
        }

        [Test]
        public async Task RegisterAfterUserJoinBindsExistingSession()
        {
            var pool = PoolProvider.Shared;
            var serializer = new ServiceRegistrySessionListenerSerializer(pool);

            using var registry = new ServiceRegistry(pool, serializer);

            var clientStream = new PinionCore.Network.Stream();
            var gatewayUserStream = new PinionCore.Network.ReverseStream(clientStream);
            var session = new UserSession(7, gatewayUserStream, pool);
            registry.Join(session);

            var serviceStream = new PinionCore.Network.Stream();
            var gatewayStream = new PinionCore.Network.ReverseStream(serviceStream);
            registry.Register(DefaultGroup, gatewayStream);

            var serviceReader = new PinionCore.Network.PackageReader(serviceStream, pool);
            var joinBuffers = await serviceReader.Read();
            Assert.That(joinBuffers.Count, Is.GreaterThan(0));
            var joinPackage = (ServiceRegistryPackage)serializer.Deserialize(joinBuffers[0]);
            Assert.That(joinPackage.OpCode, Is.EqualTo(OpCodeFromServiceRegistry.Join));
            Assert.That(joinPackage.UserId, Is.EqualTo(session.Id));
        }

        [Test]
        public async Task UnregisterReassignsUserToRemainingService()
        {
            var pool = PoolProvider.Shared;
            var serializer = new ServiceRegistrySessionListenerSerializer(pool);

            using var registry = new ServiceRegistry(pool, serializer);

            var serviceStreamA = new PinionCore.Network.Stream();
            var gatewayStreamA = new PinionCore.Network.ReverseStream(serviceStreamA);
            registry.Register(DefaultGroup, gatewayStreamA);
            var readerA = new PinionCore.Network.PackageReader(serviceStreamA, pool);

            var sessionStream = new PinionCore.Network.Stream();
            var sessionGatewayStream = new PinionCore.Network.ReverseStream(sessionStream);
            var session = new UserSession(9, sessionGatewayStream, pool);
            registry.Join(session);

            var initialJoin = await readerA.Read();
            Assert.That(initialJoin.Count, Is.GreaterThan(0));

            var serviceStreamB = new PinionCore.Network.Stream();
            var gatewayStreamB = new PinionCore.Network.ReverseStream(serviceStreamB);
            registry.Register(DefaultGroup, gatewayStreamB);
            var readerB = new PinionCore.Network.PackageReader(serviceStreamB, pool);

            registry.Unregister(DefaultGroup, gatewayStreamA);

            var reassigned = await readerB.Read();
            Assert.That(reassigned.Count, Is.GreaterThan(0));
            var reassignedPackage = (ServiceRegistryPackage)serializer.Deserialize(reassigned[0]);
            Assert.That(reassignedPackage.OpCode, Is.EqualTo(OpCodeFromServiceRegistry.Join));
            Assert.That(reassignedPackage.UserId, Is.EqualTo(session.Id));
        }

        private static (uint Group, byte[] Payload) ReadMessage(Memorys.Buffer buffer)
        {
            var segment = buffer.Bytes;
            if (segment.Count < sizeof(uint))
            {
                return (0, Array.Empty<byte>());
            }

            var group = BinaryPrimitives.ReadUInt32LittleEndian(segment.Array.AsSpan(segment.Offset, sizeof(uint)));
            var payload = new byte[segment.Count - sizeof(uint)];
            if (payload.Length > 0)
            {
                Array.Copy(segment.Array, segment.Offset + sizeof(uint), payload, 0, payload.Length);
            }

            return (group, payload);
        }
    }
}
