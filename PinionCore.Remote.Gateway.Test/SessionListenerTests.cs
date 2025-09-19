using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using PinionCore.Memorys;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway.Tests
{
    public class SessionListenerTests
    {
        [Test, Timeout(10000)]
        public async System.Threading.Tasks.Task Test()
        {
            var stream = new PinionCore.Network.Stream();
            var gatewayStream = new PinionCore.Network.ReverseStream(stream);
            var gatewayReader = new PinionCore.Network.PackageReader(gatewayStream, PoolProvider.Shared);
            var gatewayWriter = new PinionCore.Network.PackageSender(gatewayStream, PoolProvider.Shared);

            var serializer = new ServiceRegistrySessionListenerSerializer(PoolProvider.Shared);
            var sessionListener = new PinionCore.Remote.Gateway.SessionListener(stream, PoolProvider.Shared, serializer);
            var userProvider = new PinionCore.Remote.Soul.UserProvider(sessionListener, PoolProvider.Shared);

            using var leaveEvent = new ManualResetEventSlim(false);
            userProvider.LeaveEvent += (_) => {
                Console.WriteLine("LeaveEvent triggered");
                leaveEvent.Set();
            };

            using var enterEvent = new ManualResetEventSlim(false);
            userProvider.JoinEvent += (_,_,_) => {
                Console.WriteLine("JoinEvent triggered");
                enterEvent.Set();
            };

            var listenableSubscriber = new ListenableSubscriber(sessionListener, PoolProvider.Shared);
            listenableSubscriber.MessageEvent += (stream,buffers, sender) =>
            {
                Console.WriteLine($"ListenableSubscriber.MessageEvent: Received {buffers.Count()} buffers");
                // revert buffers and send back
                foreach(var buffer in buffers)
                {
                    Console.WriteLine($"Original buffer: [{string.Join(", ", buffer.Bytes.ToArray())}]");
                    // revert buffer data
                    var revertBuf = PoolProvider.Shared.Alloc(buffer.Bytes.Count);
                    for(int i = 0; i < buffer.Bytes.Count; i++)
                    {
                        revertBuf[i] = buffer.Bytes[buffer.Bytes.Count - 1 - i];
                    }
                    Console.WriteLine($"Reversed buffer: [{string.Join(", ", revertBuf.Bytes.ToArray())}]");
                    sender.Push(revertBuf);
                    Console.WriteLine("Buffer sent back to sender");
                }
            };

            sessionListener.OnDisconnected += () =>
            {
                Console.WriteLine("SessionListener OnDisconnected");
            };
            Console.WriteLine("Starting SessionListener");
            sessionListener.Start();

            Console.WriteLine("Sending Join package");
            var pkg = new ServiceRegistryPackage
            {
                OpCode = OpCodeFromServiceRegistry.Join,
                UserId = 1,
                Payload = new byte[0]
            };
            var buf = serializer.Serialize(pkg);
            gatewayWriter.Push(buf);

            Console.WriteLine("Waiting for enter event");
            Assert.That(enterEvent.Wait(TimeSpan.FromSeconds(5)), Is.True);
            Console.WriteLine("Enter event received successfully");

            Console.WriteLine("Sending Message package");
            var pkg1 = new ServiceRegistryPackage
            {
                OpCode = OpCodeFromServiceRegistry.Message,
                UserId = 1,
                Payload = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }
            };
            var buf1 = serializer.Serialize(pkg1);
            gatewayWriter.Push(buf1);
            Console.WriteLine("Reading response");
            var readedBuffers = await gatewayReader.Read();
            Console.WriteLine($"Received {readedBuffers.Count} buffers");
            Assert.That(readedBuffers.Count, Is.GreaterThan(0));

            var responsePackage = (SessionListenerPackage)serializer.Deserialize(readedBuffers[0]);
            Console.WriteLine($"Response OpCode: {responsePackage.OpCode}, UserId: {responsePackage.UserId}, Payload length: {responsePackage.Payload?.Length ?? 0}");
            Assert.That(responsePackage.OpCode, Is.EqualTo(OpCodeFromSessionListener.Message));
            Assert.That(responsePackage.UserId, Is.EqualTo(1));
            Assert.IsTrue(responsePackage.Payload.SequenceEqual(new byte[] { 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 }));
            Console.WriteLine("Message test passed");

            Console.WriteLine("Sending Leave package");
            var pkg2 = new ServiceRegistryPackage
            {
                OpCode = OpCodeFromServiceRegistry.Leave,
                UserId = 1,
                Payload = new byte[0]
            };
            var buf2 = serializer.Serialize(pkg2);
            gatewayWriter.Push(buf2);

            Console.WriteLine("Waiting for leave event");
            Assert.That(leaveEvent.Wait(TimeSpan.FromSeconds(5)), Is.True);
            Console.WriteLine("Leave event received successfully");
        }
    }
}

