using Microsoft.Win32;
using NUnit.Framework;
using PinionCore.Remote.Standalone;


namespace PinionCore.Remote.Gateway.Tests
{
    public class Tests
    {
        [NUnit.Framework.Test, Timeout(10000)]
        public async System.Threading.Tasks.Task GatewayRegistryAgentIntegrationTestAsync()
        {

            // step.1: 建立 Host
            using var host = new Host();

            // step.2: 建立遊戲服務
            using var gameService1 = (new TestGameEntry(gameType: TestGameEntry.GameType.Method1)).ToService();
            using var gameService2 = (new TestGameEntry(gameType: TestGameEntry.GameType.Method2)).ToService();

            // step.3: 建立註冊中心
            // 接收來自 Gateway Host 的連線請求
            using var registry1 = new PinionCore.Remote.Gateway.Registry(1);            
            using var registryAgentWorker1 = new AgentWorker(registry1.Agent);
            using var registry2 = new PinionCore.Remote.Gateway.Registry(2);
            using var registryAgentWorker2 = new AgentWorker(registry2.Agent);

            // step.4: 註冊遊戲服務
            // gameService 接收來自用戶的連線
            registry1.Listener.StreamableEnterEvent += gameService1.Join;
            registry1.Listener.StreamableLeaveEvent += gameService1.Leave;
            registry2.Listener.StreamableEnterEvent += gameService2.Join;
            registry2.Listener.StreamableLeaveEvent += gameService2.Leave;

            // step.5: 連線Host (單機)
            registry1.Agent.Connect(host.RegistryService);
            registry2.Agent.Connect(host.RegistryService);

            // step.6: 建立前端            
            var agent = new Agent(new Hosts.AgentPool(PinionCore.Remote.Tools.Protocol.Sources.TestCommon.ProtocolProvider.CreateCase1()));
            using var agentWorker = new AgentWorker(agent);
            // step.7: 連線 Host (單機)
            agent.Connect(host.HubService);
            var gameClient = new TestGameClient(agent);

            // step.8: 取得遊戲服務的資料
            var value1 = await gameClient.GetMethod1();
            Assert.AreEqual(1,value1);
            var value2 = await gameClient.GetMethod2();
            Assert.AreEqual(2, value2);

            // release ...

            registry1.Listener.StreamableEnterEvent -= gameService1.Join;
            registry1.Listener.StreamableLeaveEvent -= gameService1.Leave;
            registry2.Listener.StreamableEnterEvent -= gameService2.Join;
            registry2.Listener.StreamableLeaveEvent -= gameService2.Leave;







        }
    }
}


