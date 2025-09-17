using NUnit.Framework;

namespace PinionCore.Remote.Gateway.Tests
{
    public class GatewayServerTests
    {
        [Test] 
        public void Test()
        {
            var pool = PinionCore.Memorys.PoolProvider.Shared;
            var channel = new Sessions.SessionChannel(pool);
            var listenableSub = new ListenableSubscriber(channel.Listenable, pool);

            listenableSub.MessageEvent +=(session, buffers, sender) =>
            {
                foreach (var buffer in buffers)
                {
                    // Process each buffer
                }
            };

            var connector = channel.SpawnConnector(new PinionCore.Network.Stream());

            var server = new PinionCore.Remote.Gateway.Server();
        
            server.Register(1, connector);
            server.Unregister(connector);
            /*

            server.Join();
            server.Leave();*/
        }
    }
}

