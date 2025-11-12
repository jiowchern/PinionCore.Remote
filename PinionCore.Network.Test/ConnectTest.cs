using System.Linq;
using NUnit.Framework;
using PinionCore.Remote;

namespace PinionCore.Network.Tests
{
    public class StreamsTest
    {
        //[Test()]    
        public void CommunicationDevicePushTestMutli()
        {
            System.Collections.Generic.IEnumerable<System.Threading.Tasks.Task> tasks = from _ in System.Linq.Enumerable.Range(0, 10000000)
                                                                                        select CommunicationDevicePushTest();

            System.Threading.Tasks.Task.WhenAll(tasks);





        }
        [Test(/*Timeout = 5000*/)]
        public async System.Threading.Tasks.Task CommunicationDevicePushTest()
        {
            var sendBuf = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var recvBuf = new byte[11];

            var cd = new Stream();
            var peer = cd as IStreamable;
            await cd.Push(sendBuf, 0, sendBuf.Length);


            var receiveCount1 = await peer.Receive(recvBuf, 0, 4);
            var receiveCount2 = await peer.Receive(recvBuf, 4, 5);
            var receiveCount3 = await peer.Receive(recvBuf, 9, 2);

            Assert.AreEqual(4, receiveCount1);
            Assert.AreEqual(5, receiveCount2);
            Assert.AreEqual(1, receiveCount3);
        }

        [Test(), Timeout(5000)]
        public async System.Threading.Tasks.Task CommunicationDevicePopTest()
        {
            var sendBuf = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var recvBuf = new byte[10];

            var cd = new Stream();
            var peer = cd as IStreamable;

            IAwaitableSource<int> result1 = peer.Send(sendBuf, 0, 4);
            var sendResult1 = await result1;

            IAwaitableSource<int> result2 = peer.Send(sendBuf, 4, 6);
            var sendResult2 = await result2;


            IAwaitableSource<int> streamTask1 = cd.Pop(recvBuf, 0, 3);
            var stream1 = await streamTask1;

            IAwaitableSource<int> streamTask2 = cd.Pop(recvBuf, stream1, recvBuf.Length - stream1);
            var stream2 = await streamTask2;

            //var streamTask3 = cd.Pop(recvBuf, stream1 + stream2, recvBuf.Length - (stream1 + stream2));
            //int stream3 = await streamTask3;



            Assert.AreEqual(10, /*stream3 + */stream2 + stream1);
            Assert.AreEqual((byte)0, recvBuf[0]);
            Assert.AreEqual((byte)1, recvBuf[1]);
            Assert.AreEqual((byte)2, recvBuf[2]);
            Assert.AreEqual((byte)3, recvBuf[3]);
            Assert.AreEqual((byte)4, recvBuf[4]);
            Assert.AreEqual((byte)5, recvBuf[5]);
            Assert.AreEqual((byte)6, recvBuf[6]);
            Assert.AreEqual((byte)7, recvBuf[7]);
            Assert.AreEqual((byte)8, recvBuf[8]);
            Assert.AreEqual((byte)9, recvBuf[9]);

        }
        
    }
    public class ConnectTest
    {
        [NUnit.Framework.Test]
        public async System.Threading.Tasks.Task ConnectSuccessTest()
        {
            var port = PinionCore.Network.Tcp.Tools.GetAvailablePort();

            var lintener = new PinionCore.Network.Tcp.Listener();
            lintener.AcceptEvent += (peer) => { NUnit.Framework.Assert.IsNotNull(peer); };
            lintener.Bind(port,10);
            var connector = new PinionCore.Network.Tcp.Connector();

            Tcp.Peer peer = await connector.ConnectAsync(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, port));

            NUnit.Framework.Assert.IsNotNull(peer);

            if (false)
            {
                // disconnect test
                await peer.Disconnect(true);

                // reconnect test
                peer = await connector.ConnectAsync(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, port));

                NUnit.Framework.Assert.IsNotNull(peer);
            }
            else
            {
                // disconnect test
                await peer.Disconnect(false);
            }

            lintener.Close();
        }

        //[NUnit.Framework.Test]
        //public async System.Threading.Tasks.Task ConnectFailTest()
        //{
        //    var port = PinionCore.Network.Tcp.Tools.GetAvailablePort();

        //    var connector = new PinionCore.Network.Tcp.Connector();
        //    System.AggregateException ex = await connector.Connect(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, port)).ContinueWith(t =>
        //    {
        //        t.Exception.Handle(e =>
        //        {
        //            NUnit.Framework.Assert.IsNotNull(e);
        //            return true;
        //        });

        //        return t.Exception;
        //    });

        //    NUnit.Framework.Assert.IsNotNull(ex);
        //}

        [NUnit.Framework.Test]
        public async System.Threading.Tasks.Task DisconnectTest()
        {
            var port = PinionCore.Network.Tcp.Tools.GetAvailablePort();

            var lintener = new PinionCore.Network.Tcp.Listener();
            var breakEvent = false;
            var acceptTcs = new System.Threading.Tasks.TaskCompletionSource<PinionCore.Network.Tcp.Peer>(System.Threading.Tasks.TaskCreationOptions.RunContinuationsAsynchronously);
            var breakEventTcs = new System.Threading.Tasks.TaskCompletionSource<bool>(System.Threading.Tasks.TaskCreationOptions.RunContinuationsAsynchronously);

            lintener.AcceptEvent += (peer) =>
            {
                peer.BreakEvent += () =>
                {
                    breakEvent = true;
                    breakEventTcs.TrySetResult(true);
                };

                acceptTcs.TrySetResult(peer);
            };
            lintener.Bind(port,10);
            var connector = new PinionCore.Network.Tcp.Connector();

            PinionCore.Network.Tcp.Peer peer = await connector.ConnectAsync(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, port));

            var serverPeer = await acceptTcs.Task.WaitAsync(System.TimeSpan.FromSeconds(1));

            {
                IStreamable streamable = peer;
                var buffer = new byte[1024];
                var count = await streamable.Send(buffer, 0, buffer.Length);
            }

            await peer.Disconnect(false);

            {
                IStreamable streamable = serverPeer;
                var buffer = new byte[1024];
                var count = await streamable.Receive(buffer, 0, buffer.Length);
                var count2 = await streamable.Receive(buffer, 0, buffer.Length);
            }

            await breakEventTcs.Task.WaitAsync(System.TimeSpan.FromSeconds(1));

            lintener.Close();

            NUnit.Framework.Assert.AreEqual(true, breakEvent);
        }
    }
}
