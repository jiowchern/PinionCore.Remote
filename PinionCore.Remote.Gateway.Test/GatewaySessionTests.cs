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

            var reverseStream = new PinionCore.Network.ReverseStream(stream);
            var reverseReader = new PinionCore.Network.PackageReader(reverseStream, PinionCore.Memorys.PoolProvider.Shared);
            var reverseSender = new PinionCore.Network.PackageSender(reverseStream, PinionCore.Memorys.PoolProvider.Shared);
            var connector = new PinionCore.Remote.Gateway.Sessions.GatewaySessionConnector(reverseReader, reverseSender, serializer);

            var client1Stream = new PinionCore.Network.Stream();
            var client1Reader = new PinionCore.Network.PackageReader(client1Stream, PinionCore.Memorys.PoolProvider.Shared);
            var client1Sender = new PinionCore.Network.PackageSender(client1Stream, PinionCore.Memorys.PoolProvider.Shared);

            var listenable = listener as IListenable;

            var disposables = new List<IDisposable>();
            _Subscribe(listenable, disposables);

            var testData = new byte[] { 1, 2, 3, 4, 5 };
            // Important: pass a reversed view of client stream to match directions
            connector.Join(new PinionCore.Network.ReverseStream(client1Stream));            
            client1Sender.Push(_CreateBuffer(PinionCore.Memorys.PoolProvider.Shared, testData));

            var waiter = client1Reader.Read().GetAwaiter();
            while (!waiter.IsCompleted)
            {
                listener.HandlePackages();
                connector.HandlePackages();
                await Task.Delay(10);
            }

            foreach (var d in disposables)
            {
                d.Dispose();
            }
            var buffers = waiter.GetResult();
            Assert.AreEqual(1, buffers.Count);
            var result = buffers[0].Bytes.Array.ToArray();
            Array.Reverse(result);
            Assert.AreEqual(testData, result);
        }

        private void _Subscribe(IListenable listenable , ICollection<IDisposable> disposables)
        {
            var obs = from str in System.Reactive.Linq.Observable.FromEvent<System.Action<Network.IStreamable>, Network.IStreamable>(
                                h => listenable.StreamableEnterEvent += h,
                                h => listenable.StreamableEnterEvent -= h)
                       select str;
            disposables.Add(obs.Subscribe(s => { _Subscribe(s, disposables); }));             
        }

        private void _Subscribe(IStreamable streamable, ICollection<IDisposable> disposables)
        {
            var reader = new PinionCore.Network.PackageReader(streamable, PinionCore.Memorys.PoolProvider.Shared);
            var sender = new PinionCore.Network.PackageSender(streamable, PinionCore.Memorys.PoolProvider.Shared);
            var obs = from buf in System.Reactive.Linq.Observable.FromAsync(() => reader.Read())
                      from b in buf
                      select b;
            disposables.Add(obs.Subscribe(b =>
            {
                Debug.WriteLine($"Received {b.Bytes.Array.Length} bytes");
                // reverse and send back
                var buf = PinionCore.Memorys.PoolProvider.Shared.Alloc(b.Bytes.Count);
                for (var i = 0; i < b.Bytes.Count; i++)
                {
                    buf[i] = b[b.Bytes.Count - 1 - i];
                }
                sender.Push(buf);
            }));
        }

        private Memorys.Buffer _CreateBuffer(Pool shared, byte[] bytes)
        {
            var buffer = shared.Alloc(bytes.Length);
            Array.Copy(bytes, buffer.Bytes.Array, bytes.Length);
            return buffer;
        }
    }
}

