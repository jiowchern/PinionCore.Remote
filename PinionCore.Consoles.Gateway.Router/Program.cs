using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PinionCore.Consoles.Gateway.Router.Configuration;
using PinionCore.Consoles.Gateway.Router.Infrastructure;
using PinionCore.Consoles.Gateway.Router.Services;
using PinionCore.Consoles.Gateway.Router.Workers;

namespace PinionCore.Consoles.Gateway.Router
{
    class Program
    {

        static async Task<int> Main(string[] args)
        {
            // 初始化日誌配置
            using var loggingConfig = new LoggingConfiguration("RouterConsole");
            var log = loggingConfig.Log;

            // 設置優雅關閉處理器
            var shutdownHandler = new GracefulShutdownHandler(TimeSpan.FromSeconds(20));
            shutdownHandler.Register(log);

            RouterService? routerService = null;
            AgentListenerService? agentListener = null;
            RegistryListenerService? registryListener = null;
            AgentWorkerPool? workerPool = null;

            try
            {
                // T013: 解析命令列參數
                var configuration = new ConfigurationBuilder()
                    .AddCommandLine(args)
                    .Build();

                var options = new RouterOptions();

                // 綁定命令列參數到 Options
                if (int.TryParse(configuration["agent-tcp-port"], out int agentTcpPort))
                    options.AgentTcpPort = agentTcpPort;

                if (int.TryParse(configuration["agent-web-port"], out int agentWebPort))
                    options.AgentWebPort = agentWebPort;

                if (int.TryParse(configuration["registry-tcp-port"], out int registryTcpPort))
                    options.RegistryTcpPort = registryTcpPort;

                // T019: 端口配置驗證
                if (!options.Validate(out string error))
                {
                    log.WriteInfo(() => $"配置驗證失敗: {error}");
                    Console.WriteLine(RouterOptions.GetUsageString());
                    return 1;
                }

                log.WriteInfo(() => $"Router 配置: Agent TCP={options.AgentTcpPort}, Agent WebSocket={options.AgentWebPort}, Registry TCP={options.RegistryTcpPort}");

                // T017: 初始化 Router 服務
                workerPool = new AgentWorkerPool();
                routerService = new RouterService(log);

                // T015: 初始化 Agent 監聽器服務
                agentListener = new AgentListenerService(log, workerPool);

                // T016: 初始化 Registry 監聽器服務
                registryListener = new RegistryListenerService(log);

                // T018, T020: 綁定監聽器 (含端口衝突偵測)
                try
                {
                    agentListener.Start(routerService.SessionEndpoint, options.AgentTcpPort, options.AgentWebPort);
                }
                catch (Exception ex)
                {
                    log.WriteInfo(() => $"Agent 監聽器綁定失敗 (端口 {options.AgentTcpPort}/{options.AgentWebPort}): {ex.Message}");
                    log.WriteInfo("可能原因: 端口已被占用。請使用 netstat -an 檢查端口狀態或使用不同端口重試。");
                    return 1;
                }

                try
                {
                    registryListener.Start(routerService.RegistryEndpoint, options.RegistryTcpPort);
                }
                catch (Exception ex)
                {
                    log.WriteInfo(() => $"Registry 監聽器綁定失敗 (端口 {options.RegistryTcpPort}): {ex.Message}");
                    log.WriteInfo("可能原因: 端口已被占用。請使用 netstat -an 檢查端口狀態或使用不同端口重試。");
                    return 1;
                }

                log.WriteInfo("Router Console 啟動完成，所有監聽器已就緒");

                // T023: 使用事件驅動架構，無需 Update 迴圈
                // 只需保持主執行緒運行，等待關閉訊號
                await Task.Delay(Timeout.Infinite, shutdownHandler.ShutdownToken);

                return 0;
            }
            catch (OperationCanceledException)
            {
                // 收到關閉訊號，執行優雅關閉
                log.WriteInfo("收到關閉訊號，開始優雅關閉...");

                // T024, T025: 優雅關閉邏輯
                await ShutdownAsync(log, agentListener, registryListener, workerPool, routerService, loggingConfig);

                log.WriteInfo("優雅關閉完成");
                return 0;
            }
            catch (Exception ex)
            {
                log.WriteInfo(() => $"應用程式啟動失敗: {ex.Message}");
                return 1;
            }
        }

        /// <summary>
        /// T024, T025: 優雅關閉流程
        /// </summary>
        private static async Task ShutdownAsync(
            PinionCore.Utility.Log log,
            AgentListenerService? agentListener,
            RegistryListenerService? registryListener,
            AgentWorkerPool? workerPool,
            RouterService? routerService,
            LoggingConfiguration loggingConfig)
        {
            var shutdownCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            try
            {
                // 步驟 1: 關閉監聽器 (停止接受新連線)
                log.WriteInfo("關閉監聽器...");
                agentListener?.Dispose();
                registryListener?.Dispose();
                await Task.Delay(100, shutdownCts.Token);

                // 步驟 2: 關閉 AgentWorkerPool (關閉現有連線)
                if (workerPool != null)
                {
                    log.WriteInfo(() => $"關閉 {workerPool.Count} 個 Agent 連線...");
                    await workerPool.DisposeAllAsync(shutdownCts.Token);
                }

                // 步驟 3: 關閉 Router
                log.WriteInfo("關閉 Router 服務...");
                routerService?.Dispose();
                await Task.Delay(100, shutdownCts.Token);

                // 步驟 4: 關閉日誌系統 (T022)
                log.WriteInfo("寫入日誌檔案...");
                loggingConfig.Dispose();
            }
            catch (OperationCanceledException)
            {
                log.WriteInfo("優雅關閉超時 (20 秒)，強制終止");
            }
            catch (Exception ex)
            {
                log.WriteInfo(() => $"優雅關閉發生錯誤: {ex.Message}");
            }
        }
    }
}
