using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PinionCore.Remote.Gateway.Protocols;
using PinionCore.Remote.Gateway.Servers;
using PinionCore.Network;

namespace PinionCore.Remote.Gateway.Tests
{
    public class GatewayServerClientChannelTests
    {
        [Test]
        public void ResponseEvent_AllowsSingleSubscriptionOnly()
        {
            var channel = new GatewayServerClientChannel(1);
            var connection = (IConnection)channel;
            Action<byte[]> handler = _ => { };

            connection.ResponseEvent += handler;

            Assert.Throws<InvalidOperationException>(() => connection.ResponseEvent += _ => { });
        }

        [Test]
        public void ResponseEvent_AllowsResubscribeAfterRemoval()
        {
            var channel = new GatewayServerClientChannel(2);
            var connection = (IConnection)channel;
            Action<byte[]> handler = _ => { };

            connection.ResponseEvent += handler;
            connection.ResponseEvent -= handler;

            Assert.DoesNotThrow(() => connection.ResponseEvent += handler);
        }

        [Test, Timeout(5000)]
        public void Send_DeliversQueuedDataAfterSubscription()
        {
            var channel = new GatewayServerClientChannel(3);
            var connection = (IConnection)channel;
            var streamable = (IStreamable)channel;
            var payload = new byte[] { 1, 2, 3 };
            var received = new ManualResetEventSlim();
            byte[] delivered = null;

            streamable.Send(payload, 0, payload.Length);

            connection.ResponseEvent += data =>
            {
                delivered = data;
                received.Set();
            };

            Assert.That(received.Wait(1000), Is.True, "Expected queued data to flush after subscription.");
            Assert.IsNotNull(delivered);
            CollectionAssert.AreEqual(payload, delivered);
        }

        [Test, Timeout(5000)]
        public void Send_ReentrantSendFlushesAllData()
        {
            var channel = new GatewayServerClientChannel(4);
            var connection = (IConnection)channel;
            var streamable = (IStreamable)channel;
            var payload1 = new byte[] { 0, 1, 2, 3 };
            var payload2 = new byte[] { 4, 5, 6, 7 };
            var completion = new ManualResetEventSlim();
            var aggregated = new List<byte>();
            var reentered = 0;

            Action<byte[]> handler = null;
            handler = data =>
            {
                lock (aggregated)
                {
                    aggregated.AddRange(data);
                    if (aggregated.Count >= payload1.Length + payload2.Length)
                    {
                        completion.Set();
                    }
                }

                if (Interlocked.CompareExchange(ref reentered, 1, 0) == 0)
                {
                    streamable.Send(payload2, 0, payload2.Length);
                }
            };

            connection.ResponseEvent += handler;
            streamable.Send(payload1, 0, payload1.Length);

            Assert.That(completion.Wait(1000), Is.True, "Expected both payloads to be delivered.");

            byte[] snapshot;
            lock (aggregated)
            {
                snapshot = aggregated.ToArray();
            }

            Assert.That(snapshot.Length, Is.GreaterThanOrEqualTo(payload1.Length + payload2.Length));
            CollectionAssert.AreEqual(payload1, snapshot.Take(payload1.Length).ToArray());
            CollectionAssert.AreEqual(payload2, snapshot.Skip(payload1.Length).Take(payload2.Length).ToArray());
        }

        [Test, Timeout(10000)]
        public void Send_ConcurrentSendDeliversAllBytes()
        {
            const int messageCount = 64;
            var channel = new GatewayServerClientChannel(5);
            var connection = (IConnection)channel;
            var streamable = (IStreamable)channel;
            var expected = new List<byte>();
            foreach (var i in Enumerable.Range(0, messageCount))
            {
                expected.AddRange(BitConverter.GetBytes(i));
            }

            var aggregated = new List<byte>();
            var allReceived = new ManualResetEventSlim();
            connection.ResponseEvent += data =>
            {
                lock (aggregated)
                {
                    aggregated.AddRange(data);
                    if (aggregated.Count >= expected.Count)
                    {
                        allReceived.Set();
                    }
                }
            };

            Parallel.For(0, messageCount, i =>
            {
                var payload = BitConverter.GetBytes(i);
                streamable.Send(payload, 0, payload.Length);
            });

            Assert.That(allReceived.Wait(2000), Is.True, "Expected to receive all bytes.");

            byte[] snapshot;
            lock (aggregated)
            {
                snapshot = aggregated.ToArray();
            }

            Assert.AreEqual(expected.Count, snapshot.Length);
            Assert.That(snapshot.Length % sizeof(int), Is.Zero, "Payload aggregation should align with int boundaries.");

            var values = new List<int>(snapshot.Length / sizeof(int));
            for (var offset = 0; offset < snapshot.Length; offset += sizeof(int))
            {
                values.Add(BitConverter.ToInt32(snapshot, offset));
            }

            values.Sort();
            CollectionAssert.AreEqual(Enumerable.Range(0, messageCount), values);
        }
    }
}
