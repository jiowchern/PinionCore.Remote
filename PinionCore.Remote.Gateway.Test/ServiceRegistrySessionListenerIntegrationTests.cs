using System;
using System.Buffers.Binary;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PinionCore.Memorys;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway.Tests
{
    public class ServiceRegistrySessionListenerIntegrationTests
    {
        private const uint DefaultGroup = 1;

        [Test, Timeout(10000)]
        public async Task ServiceRegistryAndSessionListenerExchangeMessages()
        {
            var pool = PoolProvider.Shared;
            var serializer = new ServiceRegistrySessionListenerSerializer(pool);

            var serviceStream = new PinionCore.Network.Stream();
            var gatewayStream = new PinionCore.Network.ReverseStream(serviceStream);

            using var sessionListener = new SessionListener(serviceStream, pool, serializer);
            using var registry = new ServiceRegistry(pool, serializer);
            using var userProvider = new UserProvider(sessionListener, pool);
            using var subscriber = new ListenableSubscriber(sessionListener, pool);

            using var joinEvent = new ManualResetEventSlim(false);
            using var leaveEvent = new ManualResetEventSlim(false);
            using var serverMessageEvent = new ManualResetEventSlim(false);

            var initialPayload = new byte[] { 9, 8, 7 };

            userProvider.JoinEvent += (_, reader, sender) =>
            {
                var buffer = pool.Alloc(initialPayload.Length + sizeof(uint));
                var segment = buffer.Bytes;
                BinaryPrimitives.WriteUInt32LittleEndian(segment.Array.AsSpan(segment.Offset, sizeof(uint)), DefaultGroup);
                Array.Copy(initialPayload, 0, segment.Array, segment.Offset + sizeof(uint), initialPayload.Length);
                sender.Push(buffer);
                joinEvent.Set();
            };
            userProvider.LeaveEvent += _ => leaveEvent.Set();

            subscriber.MessageEvent += (_, buffers, sender) =>
            {
                serverMessageEvent.Set();
                foreach (var buffer in buffers)
                {
                    var segment = buffer.Bytes;
                    if (segment.Count < sizeof(uint))
                    {
                        continue;
                    }

                    var group = BinaryPrimitives.ReadUInt32LittleEndian(segment.Array.AsSpan(segment.Offset, sizeof(uint)));
                    var payloadLength = segment.Count - sizeof(uint);
                    var echo = pool.Alloc(segment.Count);
                    var echoSegment = echo.Bytes;
                    BinaryPrimitives.WriteUInt32LittleEndian(echoSegment.Array.AsSpan(echoSegment.Offset, sizeof(uint)), group);
                    Array.Copy(segment.Array, segment.Offset + sizeof(uint), echoSegment.Array, echoSegment.Offset + sizeof(uint), payloadLength);
                    sender.Push(echo);
                }
            };

            sessionListener.Start();
            registry.Register(DefaultGroup, gatewayStream);

            var clientStream = new PinionCore.Network.Stream();
            var gatewayUserStream = new PinionCore.Network.ReverseStream(clientStream);
            var userReader = new PinionCore.Network.PackageReader(clientStream, pool);
            var userSender = new PinionCore.Network.PackageSender(clientStream, pool);

            var session = new UserSession(99, gatewayUserStream, pool);
            registry.Join(session);

            Assert.That(joinEvent.Wait(TimeSpan.FromSeconds(5)), Is.True);

            var initialBuffers = await userReader.Read();
            Assert.That(initialBuffers.Count, Is.GreaterThan(0));
            var initialMessages = initialBuffers.Select(ReadMessage).ToList();
            Assert.That(initialMessages.Any(m => m.Group == DefaultGroup && EndsWithPayload(m.Payload, initialPayload)), Is.True);

            var clientPayload = new byte[] { 1, 2, 3, 4 };
            var clientBuffer = pool.Alloc(clientPayload.Length + sizeof(uint));
            var clientSegment = clientBuffer.Bytes;
            BinaryPrimitives.WriteUInt32LittleEndian(clientSegment.Array.AsSpan(clientSegment.Offset, sizeof(uint)), DefaultGroup);
            Array.Copy(clientPayload, 0, clientSegment.Array, clientSegment.Offset + sizeof(uint), clientPayload.Length);
            userSender.Push(clientBuffer);

            Assert.That(serverMessageEvent.Wait(TimeSpan.FromSeconds(5)), Is.True);

            var responseBuffers = await userReader.Read();
            Assert.That(responseBuffers.Count, Is.GreaterThan(0));
            var responseMessages = responseBuffers.Select(ReadMessage).ToList();
            Assert.That(responseMessages.Any(m => m.Group == DefaultGroup && m.Payload.Length >= clientPayload.Length), Is.True);

            registry.Leave(session);
            Assert.That(leaveEvent.Wait(TimeSpan.FromSeconds(5)), Is.True);
        }

        private static bool EndsWithPayload(byte[] actual, byte[] expected)
        {
            if (actual == null || actual.Length < expected.Length)
            {
                return false;
            }

            for (var i = 0; i < expected.Length; i++)
            {
                if (actual[actual.Length - expected.Length + i] != expected[i])
                {
                    return false;
                }
            }

            return true;
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
