using System.Threading;
using NUnit.Framework;
using PinionCore.Remote.Gateway.Servers;



namespace PinionCore.Remote.Gateway.Tests
{
    public class ClientConnectionDisposerTests
    {
        [NUnit.Framework.Test, Timeout(10000)]
        public async System.Threading.Tasks.Task RecycledConnectionIdTest()
        {
            var clientConnectionDisposer = new PinionCore.Remote.Gateway.Hosts.ClientConnectionDisposer(new Hosts.RoundRobinGameLobbySelectionStrategy());

            var lobby1 = new GatewayServerConnectionManager();

            clientConnectionDisposer.Add(lobby1);
            
            var c1 = await clientConnectionDisposer.Require();
            Assert.AreEqual(1, c1.Id.Value); // 1 是 lobby1 的第一個連線 ID
            clientConnectionDisposer.Return(c1); // 歸還連線 1
            var c2 = await clientConnectionDisposer.Require(); // 再次要求連線，應該會拿到剛剛歸還的連線 1
            Assert.AreEqual(1, c2.Id.Value); // 確認拿到的連線 ID 是 1

            var c3 = await clientConnectionDisposer.Require();
            Assert.AreEqual(2, c3.Id.Value); // 2 是 lobby1 的第二個連線 ID
            clientConnectionDisposer.Return(c2); // 歸還連線 1
            var c4 = await clientConnectionDisposer.Require(); // 再次要求連線，應該會拿到剛剛歸還的連線 1
            Assert.AreEqual(1, c4.Id.Value); // 確認拿到的連線 ID 是 1

            clientConnectionDisposer.Remove(lobby1);
        }
    }
}



