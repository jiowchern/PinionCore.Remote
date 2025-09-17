using System;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using PinionCore.Memorys;
using PinionCore.Remote.Soul;

using System.Reactive;
using System.Reactive.Linq;
using System.Diagnostics;
using PinionCore.Network;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PinionCore.Remote.Gateway.Tests
{
    
    public class GatewaySessionTests
    {
        
        [Test, Timeout(10000)]
        public async System.Threading.Tasks.Task GatewaySessionReverseTransmissionTest()
        {
            var serializer = new Sessions.Serializer(PinionCore.Memorys.PoolProvider.Shared);

            var stream = new PinionCore.Network.Stream();
            var reader = new PinionCore.Network.PackageReader(stream, PinionCore.Memorys.PoolProvider.Shared);
            var sender = new PinionCore.Network.PackageSender(stream, PinionCore.Memorys.PoolProvider.Shared);
            
            var listener = new PinionCore.Remote.Gateway.Sessions.GatewaySessionListener(reader, sender, serializer);
            listener.Start();

            var reverseStream = new PinionCore.Network.ReverseStream(stream);
            var reverseReader = new PinionCore.Network.PackageReader(reverseStream, PinionCore.Memorys.PoolProvider.Shared);
            var reverseSender = new PinionCore.Network.PackageSender(reverseStream, PinionCore.Memorys.PoolProvider.Shared);
            var connector = new PinionCore.Remote.Gateway.Sessions.GatewaySessionConnector(reverseReader, reverseSender, serializer);
            connector.Start();

            try
            {
                var client1Stream = new PinionCore.Network.Stream();
                var client1Reader = new PinionCore.Network.PackageReader(client1Stream, PinionCore.Memorys.PoolProvider.Shared);
                var client1Sender = new PinionCore.Network.PackageSender(client1Stream, PinionCore.Memorys.PoolProvider.Shared);

                var listenable = listener as IListenable;

                using var subscriber = new ListenableSubscriber(listenable, PinionCore.Memorys.PoolProvider.Shared);
                subscriber.MessageEvent += (streamable, buffers, innerSender) =>
                {
                    foreach (var buffer in buffers)
                    {
                        Debug.WriteLine($"Received {buffer.Bytes.Array.Length} bytes");
                        var reversedBuffer = PinionCore.Memorys.PoolProvider.Shared.Alloc(buffer.Bytes.Count);
                        for (var i = 0; i < buffer.Bytes.Count; i++)
                        {
                            reversedBuffer[i] = buffer[buffer.Bytes.Count - 1 - i];
                        }
                        innerSender.Push(reversedBuffer);
                    }
                };

                var testData = new byte[] { 1, 2, 3, 4, 5 };
                connector.Join(new PinionCore.Network.ReverseStream(client1Stream));
                client1Sender.Push(_CreateBuffer(PinionCore.Memorys.PoolProvider.Shared, testData));

                var readTask = client1Reader.Read();
                var completed = await Task.WhenAny(readTask, Task.Delay(TimeSpan.FromSeconds(3))).ConfigureAwait(false);
                if (!ReferenceEquals(readTask, completed))
                {
                    Assert.Fail("Timed out waiting for echoed payload.");
                }

                var buffers = await readTask.ConfigureAwait(false);
                Assert.AreEqual(1, buffers.Count);
                var result = buffers[0].Bytes.Array.ToArray();
                Array.Reverse(result);
                Assert.AreEqual(testData, result);
            }
            finally
            {
                connector.Dispose();
                listener.Dispose();
            }
        }


        private Memorys.Buffer _CreateBuffer(Pool shared, byte[] bytes)
        {
            var buffer = shared.Alloc(bytes.Length);
            Array.Copy(bytes, buffer.Bytes.Array, bytes.Length);
            return buffer;
        }
    }
}

