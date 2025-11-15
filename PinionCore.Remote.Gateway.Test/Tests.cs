using Microsoft.Win32;
using NUnit.Framework;
using PinionCore.Remote.Standalone;
using PinionCore.Remote.Client;
using PinionCore.Remote.Server;
using PinionCore.Utility;


namespace PinionCore.Remote.Gateway.Tests
{
    using System;
    public class Tests
    {
        /// <summary>
        /// Gateway 整合測試：展示完整的 Gateway、Registry、Agent 三層架構使用範例
        ///
        /// 架構說明：
        /// 1. Router (路由閘道) - 作為中央協調者，負責路由客戶端連線到對應的遊戲服務
        /// 2. Registry (註冊中心) - 遊戲服務註冊者，將遊戲服務註冊到 Router，並提供玩家連線的監聽器
        /// 3. Agent (客戶端代理) - 玩家客戶端，透過 Router 連接到多個遊戲服務
        ///
        /// 測試流程：
        /// 1. 建立 Router，提供 Registry 和 Session 兩個端點
        /// 2. 建立兩個遊戲服務，分別提供不同的方法
        /// 3. 建立兩個 Registry，分別註冊到 Router（使用不同的 Group ID）
        /// 4. 將遊戲服務綁定到 Registry 的 Listener，以接收玩家連線
        /// 5. 客戶端連接到 Router 的 Session
        /// 6. 客戶端透過 Router 路由，同時與兩個遊戲服務通訊
        /// </summary>
        [NUnit.Framework.Test, Timeout(10000)]
        public async System.Threading.Tasks.Task GatewayRegistryAgentIntegrationTestAsync()
        {

            var testCommonProtocol = PinionCore.Remote.Tools.Protocol.Sources.TestCommon.ProtocolProvider.CreateCase1();
            var testChatProtocol = PinionCore.Consoles.Chat1.Common.ProtocolCreator.Create();

            //// ========================================
            //// 階段 1: 建立路由閘道 (Gateway Router)
            //// ========================================

            // Step 1: 建立 Router - 作為中央路由閘道
            // Router 提供兩個服務端點：
            // - Registry: 供遊戲服務註冊使用
            // - Session: 供玩家客戶端連接使用
            // Router 使用 RoundRobin 策略將客戶端路由到不同的遊戲服務
            using var host = new Router(new Hosts.RoundRobinSelector());

            //// ========================================
            //// 階段 2: 建立遊戲服務 (Game Services)
            //// ========================================

            // Step 2: 建立兩個獨立的遊戲服務
            // gameService1 提供 IMethodable1 介面（GetValue1 方法）
            // gameService2 提供 IMethodable2 介面（GetValue2 方法）
            // 這兩個服務代表不同的遊戲邏輯或服務類型
            using var gameService1 = (new TestGameEntry(gameType: TestGameEntry.GameType.Method1)).ToService();
            using var gameService2 = (new TestGameEntry(gameType: TestGameEntry.GameType.Method2)).ToService();

            using var gameService3 = (new TestChatEntry()).ToService();

            //// ========================================
            //// 階段 3: 建立註冊中心 (Registry)
            //// ========================================

            // Step 3: 建立 Registry - 遊戲服務的註冊代理
            // Registry 負責：
            // 1. 透過 Agent 連接到 Router 的 Registry，向 Router 註冊自己的 Group ID
            // 2. 提供 Listener 給遊戲服務，用於接收 Router 路由過來的玩家連線
            //
            // registry1 使用 Group ID = 1
            // registry2 使用 Group ID = 2
            // Group ID 用於 Router 進行路由決策
            using var registry1 = new PinionCore.Remote.Gateway.Registry(testCommonProtocol,1);
            using var registryAgentWorker1 = new AgentWorker(registry1.Agent);
            using var registry2 = new PinionCore.Remote.Gateway.Registry(testCommonProtocol, 2);
            using var registryAgentWorker2 = new AgentWorker(registry2.Agent);

            using var registry3 = new PinionCore.Remote.Gateway.Registry(testChatProtocol, 1);
            using var registryAgentWorker3 = new AgentWorker(registry3.Agent);

            //// ========================================
            //// 階段 4: 綁定遊戲服務到 Registry
            //// ========================================

            // Step 4: 將遊戲服務綁定到 Registry 的 Listener
            // 當 Router 將玩家路由到此 Registry 時，Registry 會透過 Listener 通知遊戲服務
            // 遊戲服務透過 Join/Leave 事件處理玩家的連入/離開
            //
            // registry1.Listener -> gameService1 (提供 IMethodable1)
            // registry2.Listener -> gameService2 (提供 IMethodable2)
            registry1.Listener.StreamableEnterEvent += gameService1.Join;
            registry1.Listener.StreamableLeaveEvent += gameService1.Leave;
            registry2.Listener.StreamableEnterEvent += gameService2.Join;
            registry2.Listener.StreamableLeaveEvent += gameService2.Leave;
            registry3.Listener.StreamableEnterEvent += gameService3.Join;
            registry3.Listener.StreamableLeaveEvent += gameService3.Leave;

            //// ========================================
            //// 階段 5: Registry 向 Router 註冊
            //// ========================================

            // Step 5: Registry 連接到 Router 的 Registry
            // 連接後，Registry 會自動向 Router 註冊自己的 Group ID
            // Router 收到註冊後，會將此 Registry 加入到路由表中
            // 之後當客戶端連接時，Router 會根據策略選擇 Registry 並建立連線
            using var registryConnection1 = ConnectAgent(registry1.Agent, host.Registry);
            using var registryConnection2 = ConnectAgent(registry2.Agent, host.Registry);
            using var registryConnection3 = ConnectAgent(registry3.Agent, host.Registry);

            //// ========================================
            //// 階段 6: 建立客戶端 (Client Agent)
            //// ========================================

            // Step 6: 客戶端連接到 Router 的 Session
            // Router 會根據 RoundRobin 策略，將客戶端路由到註冊的 Registry
            // 由於有兩個 Registry，客戶端會同時與兩個遊戲服務建立連線            
            var gameClient1 = new TestClient(testCommonProtocol);
            var agent1 = gameClient1.Agent;

            // 連接到 Router 的 Session 端點 (單機)
            using var sessionConnection1 = ConnectAgent(agent1, host.Session);
            using var agentWorker1 = new AgentWorker(agent1);

            var gameClient2 = new TestClient(testChatProtocol);
            var agent2 = gameClient2.Agent;

            // 連接到 Router 的 Session 端點 (單機)
            using var sessionConnection2 = ConnectAgent(agent2, host.Session);
            using var agentWorker2 = new AgentWorker(agent2);



            //// ========================================
            //// 階段 7: 測試客戶端與遊戲服務的通訊
            //// ========================================

            // Step 7: 客戶端透過 Agent 呼叫遊戲服務的方法
            // GetMethod1() 會透過 IMethodable1 介面，與 gameService1 通訊
            // GetMethod2() 會透過 IMethodable2 介面，與 gameService2 通訊
            //
            // 重點：客戶端只需要透過一個 Agent，就能同時與多個遊戲服務通訊
            // Agent 內部的 CompositeNotifier 會自動處理多個連線的整合
            var value1 = await gameClient1.GetMethod1();
            Assert.AreEqual(1,value1);
            var value2 = await gameClient1.GetMethod2();
            Assert.AreEqual(2, value2);

            var loginResult = await gameClient2.GetLogin();
            Assert.IsTrue(loginResult);

            //// ========================================
            //// 階段 8: 清理資源
            //// ========================================

            // 解除遊戲服務與 Registry 的綁定
            // 在實際應用中，這些清理工作會在 Dispose 時自動執行
            registry1.Listener.StreamableEnterEvent -= gameService1.Join;
            registry1.Listener.StreamableLeaveEvent -= gameService1.Leave;
            registry2.Listener.StreamableEnterEvent -= gameService2.Join;
            registry2.Listener.StreamableLeaveEvent -= gameService2.Leave;
            registry3.Listener.StreamableEnterEvent -= gameService3.Join;
            registry3.Listener.StreamableLeaveEvent -= gameService3.Leave;
            

        }
        private static IDisposable ConnectAgent(Remote.Ghost.IAgent agent, PinionCore.Remote.Soul.IService service)
        {
            var endpoint = new PinionCore.Remote.Standalone.ListeningEndpoint();
            var (handle, errors) = service.ListenAsync(endpoint).GetAwaiter().GetResult();
            if (errors.Length > 0)
            {
                throw new InvalidOperationException($"Standalone listener failed: {errors[0].Exception}");
            }

            var connectable = (PinionCore.Remote.Client.IConnectingEndpoint)endpoint;
            var stream = connectable.ConnectAsync().GetAwaiter().GetResult();
            agent.Enable(stream);

            return new DisposeAction(() =>
            {
                agent.Disable();
                handle.Dispose();
                ((IDisposable)endpoint).Dispose();
            });
        }
    }
}


