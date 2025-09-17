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
            
            using var listener = new PinionCore.Remote.Gateway.Sessions.GatewaySessionListener(reader, sender, serializer);

            var reverseStream = new PinionCore.Network.ReverseStream(stream);
            var reverseReader = new PinionCore.Network.PackageReader(reverseStream, PinionCore.Memorys.PoolProvider.Shared);
            var reverseSender = new PinionCore.Network.PackageSender(reverseStream, PinionCore.Memorys.PoolProvider.Shared);
            using var connector = new PinionCore.Remote.Gateway.Sessions.GatewaySessionConnector(reverseReader, reverseSender, serializer);

            var client1Stream = new PinionCore.Network.Stream();
            var client1Reader = new PinionCore.Network.PackageReader(client1Stream, PinionCore.Memorys.PoolProvider.Shared);
            var client1Sender = new PinionCore.Network.PackageSender(client1Stream, PinionCore.Memorys.PoolProvider.Shared);

            var listenable = listener as IListenable;

            var subscriber = new ListenableSubscriber(listenable, PinionCore.Memorys.PoolProvider.Shared);
            subscriber.MessageEvent += (streamable, buffers, sender) =>
            {
                foreach (var buffer in buffers)
                {
                    Debug.WriteLine($"Received {buffer.Bytes.Array.Length} bytes");
                    // reverse and send back
                    var reversedBuffer = PinionCore.Memorys.PoolProvider.Shared.Alloc(buffer.Bytes.Count);
                    for (var i = 0; i < buffer.Bytes.Count; i++)
                    {
                        reversedBuffer[i] = buffer[buffer.Bytes.Count - 1 - i];
                    }
                    sender.Push(reversedBuffer);
                }
            };

            var testData = new byte[] { 1, 2, 3, 4, 5 };
            // Important: pass a reversed view of client stream to match directions
            connector.Join(new PinionCore.Network.ReverseStream(client1Stream));
            client1Sender.Push(_CreateBuffer(PinionCore.Memorys.PoolProvider.Shared, testData));

            var waiter = client1Reader.Read().GetAwaiter();
            while (!waiter.IsCompleted)
            {
                connector.HandlePackages();
                await Task.Delay(10);
            }

            subscriber.Dispose();
            var buffers = waiter.GetResult();
            Assert.AreEqual(1, buffers.Count);
            var result = buffers[0].Bytes.Array.ToArray();
            Array.Reverse(result);
            Assert.AreEqual(testData, result);
        }


        private Memorys.Buffer _CreateBuffer(Pool shared, byte[] bytes)
        {
            var buffer = shared.Alloc(bytes.Length);
            Array.Copy(bytes, buffer.Bytes.Array, bytes.Length);
            return buffer;
        }
    }
}

