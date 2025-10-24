using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PinionCore.Consoles.Gateway.Router.Configuration;
using PinionCore.Consoles.Gateway.Router.Infrastructure;

namespace PinionCore.Consoles.Gateway.Router
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            // 初始化日誌配置
            using var loggingConfig = new LoggingConfiguration("RouterConsole");
            // Access logging through loggingConfig wrapper

            try
            {
                // 解析命令列參數
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

                // 驗證配置
                if (!options.Validate(out string error))
                {
                    loggingConfig.WriteError($"配置驗證失敗: {error}");
                    Console.WriteLine(RouterOptions.GetUsageString());
                    return 1;
                }

                loggingConfig.WriteInfo($"Router 配置: Agent TCP={options.AgentTcpPort}, Agent WebSocket={options.AgentWebPort}, Registry TCP={options.RegistryTcpPort}");

                // TODO: 實作 Router 啟動邏輯 (Phase 3)
                loggingConfig.WriteInfo("Router Console 準備就緒 (Phase 2 基礎架構完成)");

                // 暫時保持運行
                await Task.Delay(System.Threading.Timeout.Infinite);

                return 0;
            }
            catch (Exception ex)
            {
                loggingConfig.WriteError($"應用程式啟動失敗: {ex.Message}");
                return 1;
            }
        }
    }
}
