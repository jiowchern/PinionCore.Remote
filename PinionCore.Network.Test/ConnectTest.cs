using System.Linq;

namespace PinionCore.Network.Tests
{
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

            Tcp.Peer peer = await connector.Connect(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, port));

            NUnit.Framework.Assert.IsNotNull(peer);

            if (false)
            {
                // disconnect test
                await connector.Disconnect(true);


                // reconnect test
                peer = await connector.Connect(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, port));

                NUnit.Framework.Assert.IsNotNull(peer);
            }
            else
            {
                // disconnect test
                await connector.Disconnect(false);
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
            var serverPeers = new System.Collections.Generic.List<PinionCore.Network.Tcp.Peer>();

            var breakEvent = false;
            lintener.AcceptEvent += (peer) =>
            {
                peer.BreakEvent += () => { breakEvent = true; };

                serverPeers.Add(peer);
            };
            lintener.Bind(port,10);
            var connector = new PinionCore.Network.Tcp.Connector();

            Tcp.Peer peer = await connector.Connect(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, port));

            {
                IStreamable streamable = peer;
                var buffer = new byte[1024];
                var count = await streamable.Send(buffer, 0, buffer.Length);
            }


            await connector.Disconnect(false);
            {
                IStreamable streamable = serverPeers.Single();
                var buffer = new byte[1024];
                var count = await streamable.Receive(buffer, 0, buffer.Length);
                var count2 = await streamable.Receive(buffer, 0, buffer.Length);
            }


            lintener.Close();

            NUnit.Framework.Assert.AreEqual(true, breakEvent);
        }


    }
}
