using System.Threading;

namespace PinionCore.Network.Tests
{

    public class WebSocketConnectTest
    {
        [NUnit.Framework.Test]
        public async System.Threading.Tasks.Task Test()
        {

            var listener = new PinionCore.Network.Web.Listener();

            listener.Bind("http://127.0.0.1:12345/");
            var peers = new System.Collections.Concurrent.ConcurrentQueue<Web.Peer>();

            listener.AcceptEvent += peers.Enqueue;

            var connecter = new PinionCore.Network.Web.Connecter(new System.Net.WebSockets.ClientWebSocket());
            var connectResult = await connecter.ConnectAsync("ws://127.0.0.1:12345/");

            NUnit.Framework.Assert.True(connectResult);

            var ar = new PinionCore.Utility.AutoPowerRegulator(new Utility.PowerRegulator());

            Web.Peer peer;
            while (!peers.TryDequeue(out peer))
            {
                ar.Operate(new CancellationTokenSource());
            }
            IStreamable server = peer;
            var serverReceiveBuffer = new byte[5];
            Remote.IAwaitableSource<int> serverReceiveTask = server.Receive(serverReceiveBuffer, 0, 5);
            IStreamable client = connecter;
            var clientSendCount = await client.Send(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);

            var serverReceiveCount = await serverReceiveTask;

            NUnit.Framework.Assert.AreEqual(5, serverReceiveCount);
            NUnit.Framework.Assert.AreEqual(5, clientSendCount);
        }
    }
}
