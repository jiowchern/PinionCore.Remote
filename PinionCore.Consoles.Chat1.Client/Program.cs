using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using PinionCore.Consoles.Chat1.Common;
using PinionCore.Consoles.Chat1.Client.Configuration;
using PinionCore.Utility.WindowConsoleAppliction;

namespace PinionCore.Consoles.Chat1.Client
{
    internal static class Program
    {
        private const string StandaloneSwitch = "--standalone";

        static void Main(string[] args)
        {
            // T068: 解析命令列參數
            var options = CommandLineParser.Parse(args);

            // Standalone 模式檢測
            if (args.Length >= 2 && string.Equals(args[0], StandaloneSwitch, StringComparison.OrdinalIgnoreCase))
            {
                RunStandalone(new FileInfo(args[1]));
                return;
            }

            // T069: Router 模式
            if (options.HasRouterMode)
            {
                // 驗證參數有效性
                if (!options.Validate(out var error))
                {
                    System.Console.WriteLine($"錯誤: {error}");
                    System.Console.WriteLine(ChatClientOptions.GetUsageString());
                    Environment.Exit(1);
                    return;
                }

                RunGatewayMode(options);
                return;
            }

            // T070: 直連模式 (保留原有行為)
            RunRemote(args);
        }

        private static void RunStandalone(FileInfo serviceFile)
        {
            if (serviceFile == null || !serviceFile.Exists)
            {
                System.Console.WriteLine("Standalone mode requires a valid service assembly path.");
                return;
            }

            System.Console.WriteLine("Standalone mode.");

            var protocol = ProtocolCreator.Create();
            var serviceAssembly = Assembly.LoadFrom(serviceFile.FullName);
            var entry = serviceAssembly
                .GetExportedTypes()
                .Where(type => typeof(PinionCore.Remote.IEntry).IsAssignableFrom(type) && !type.IsAbstract)
                .Select(type => Activator.CreateInstance(type) as PinionCore.Remote.IEntry)
                .Single();

            using var service = PinionCore.Remote.Standalone.Provider.CreateService(entry, protocol);
            var agent = PinionCore.Remote.Standalone.Provider.CreateAgent(protocol);
            var disconnect = PinionCore.Remote.Standalone.Provider.Connect(agent, service);

            try
            {
                var console = new StandaloneConsole(agent);
                console.Run();
            }
            finally
            {
                disconnect();
            }
        }

        private static void RunGatewayMode(ChatClientOptions options)
        {
            var protocol = ProtocolCreator.Create();

            // T069: 創建 Gateway.Agent
            var agentPool = new PinionCore.Remote.Gateway.Hosts.AgentPool(protocol);
            var agent = new PinionCore.Remote.Gateway.Agent(agentPool);

            var console = new GatewayConsole(agent);

            // T062-T063: 根據參數選擇 TCP 或 WebSocket 協議
            bool connected;
            if (options.UseWebSocket)
            {
                System.Console.WriteLine("Gateway Router mode (WebSocket).");
                System.Console.WriteLine($"Router: ws://{options.RouterHost}:{options.RouterPort}/");
                connected = console.ConnectWebSocket(options.RouterHost, options.RouterPort.Value);
            }
            else
            {
                System.Console.WriteLine("Gateway Router mode (TCP).");
                System.Console.WriteLine($"Router: {options.RouterHost}:{options.RouterPort}");
                connected = console.ConnectTcp(options.RouterHost, options.RouterPort.Value);
            }

            // T071: 連接失敗錯誤處理
            if (!connected)
            {
                System.Console.WriteLine("連接失敗，按任意鍵退出...");
                System.Console.ReadKey();
                Environment.Exit(1);
                return;
            }

            console.Run();
        }

        private static void RunRemote(string[] args)
        {
            System.Console.WriteLine("Remote mode.");
            var protocol = ProtocolCreator.Create();
            var set = PinionCore.Remote.Client.Provider.CreateTcpAgent(protocol);
            var console = new RemoteConsole(set);

            if (args?.Length == 2 && int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var port))
            {
                console.Connect(args[0], port);
            }

            console.Run();
        }
    }
}
