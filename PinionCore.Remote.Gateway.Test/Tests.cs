using Microsoft.Win32;
using NUnit.Framework;
using PinionCore.Remote.Standalone;


namespace PinionCore.Remote.Gateway.Tests
{
    public class Tests
    {
        [NUnit.Framework.Test, Timeout(10000)]
        public async System.Threading.Tasks.Task Test()
        {

            // step.1: 建立 Host
            using var host = new Host();

            // step.2: 建立遊戲服務
            using var gameService = (new TestGameEntry(gameType: TestGameEntry.GameType.Method1)).ToService();

            // step.3: 建立註冊中心
            // 接收來自 Gateway Host 的連線請求
            using var registry = new PinionCore.Remote.Gateway.Registry(1);            
            using var registryAgentWorker = new AgentWorker(registry.Agent);

            // step.4: 註冊遊戲服務
            // gameService 接收來自用戶的連線
            registry.Notifier.Base.Supply += gameService.Join;
            registry.Notifier.Base.Unsupply += gameService.Leave;

            // step.5: 連線Host (單機)
            registry.Agent.Connect(host.RegistryService);

            // step.6: 建立前端            
            var agent = new Agent(new Hosts.AgentPool(PinionCore.Remote.Tools.Protocol.Sources.TestCommon.ProtocolProvider.CreateCase1()));
            using var agentWorker = new AgentWorker(agent);
            // step.7: 連線 Host (單機)
            agent.Connect(host.HubService);
            var gameClient = new TestGameClient(agent);

            // step.8: 取得遊戲服務的資料
            var value = await gameClient.GetMethod();
            Assert.AreEqual(1,value);

            // release ...

            registry.Notifier.Base.Supply -= gameService.Join;
            registry.Notifier.Base.Unsupply -= gameService.Leave;






            
        }
    }
}


