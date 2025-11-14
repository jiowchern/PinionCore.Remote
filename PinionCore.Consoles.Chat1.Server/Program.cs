using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using PinionCore.Remote.Soul;
using PinionCore.Utility.WindowConsoleAppliction;
using PinionCore.Consoles.Chat1.Server.Configuration;
using PinionCore.Consoles.Chat1.Server.Services;

namespace PinionCore.Consoles.Chat1.Server
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            PinionCore.Utility.Log.Instance.RecordEvent += message => System.Console.WriteLine(message);
            // T084: 檢查 --help 或 -h 參數
            if (args.Length > 0 && (args[0] == "--help" || args[0] == "-h" ||
                Array.Exists(args, arg => arg == "--help" || arg == "-h")))
            {
                System.Console.WriteLine(ChatServerOptions.GetUsageString());
                return;
            }

            // T029: 解析命令列參數
            var options = CommandLineParser.Parse(args);

            // 驗證參數有效性
            if (!options.Validate(out var error))
            {
                System.Console.WriteLine($"錯誤: {error}");
                System.Console.WriteLine(ChatServerOptions.GetUsageString());
                Environment.Exit(1);
                return;
            }

            var protocol = PinionCore.Consoles.Chat1.Common.ProtocolCreator.Create();
            var entry = new PinionCore.Consoles.Chat1.Entry();

            var listeners = new CompositeListenable();
            var shutdownTasks = new List<Action>();
            var enabledModes = new List<string>();

            // T046: 直連 TCP 監聽器 (帶錯誤處理)
            if (options.TcpPort.HasValue)
            {
                try
                {
                    var tcp = new PinionCore.Remote.Server.Tcp.Listener();
                    tcp.Bind(options.TcpPort.Value);
                    listeners.Add(tcp);
                    shutdownTasks.Add(() => tcp.Close());
                    enabledModes.Add($"TCP (port {options.TcpPort.Value})");
                    System.Console.WriteLine($"[OK] TCP listener started on port {options.TcpPort.Value}");
                }
                catch (Exception ex)
                {
                    // T052: 部分監聽器啟動失敗處理 - 記錄警告但繼續運行
                    System.Console.WriteLine($"[WARNING] TCP listener failed to start on port {options.TcpPort.Value}: {ex.Message}");
                }
            }

            // T047: 直連 WebSocket 監聽器 (帶錯誤處理)
            if (options.WebPort.HasValue)
            {
                try
                {
                    var web = new PinionCore.Remote.Server.Web.Listener();
                    web.Bind($"http://*:{options.WebPort.Value}/");
                    listeners.Add(web);
                    shutdownTasks.Add(() => web.Close());
                    enabledModes.Add($"WebSocket (port {options.WebPort.Value})");
                    System.Console.WriteLine($"[OK] WebSocket listener started on port {options.WebPort.Value}");
                }
                catch (Exception ex)
                {
                    // T052: 部分監聽器啟動失敗處理 - 記錄警告但繼續運行
                    System.Console.WriteLine($"[WARNING] WebSocket listener failed to start on port {options.WebPort.Value}: {ex.Message}");
                }
            }

            // T030: Registry Client 初始化 (當提供 router-host 時)
            PinionCore.Remote.Gateway.Registry registry = null;
            RegistryConnectionManager registryConnectionManager = null;
            Action registryWorkerDispose = null;

            // T048: Gateway 路由監聽器整合 (帶錯誤處理)
            if (options.HasGatewayMode)
            {
                try
                {
                    System.Console.WriteLine($"Initializing Gateway mode: Router {options.RouterHost}:{options.RouterPort} (Group: {options.Group})");

                    // 建立 Registry Client
                    registry = new PinionCore.Remote.Gateway.Registry(protocol, options.Group);

                    // T032: 啟動 AgentWorker (持續處理 registry.Agent.HandlePackets/HandleMessage)
                    var agentWorkerRunning = true;
                    var agentWorkerTask = Task.Run(() =>
                    {
                        while (agentWorkerRunning)
                        {
                            registry.Agent.HandlePackets();
                            registry.Agent.HandleMessages();
                            System.Threading.Thread.Sleep(1); // 短暫休眠避免忙等待
                        }
                    });

                    registryWorkerDispose = () =>
                    {
                        agentWorkerRunning = false;
                        agentWorkerTask.Wait(TimeSpan.FromSeconds(2));
                    };

                    // T049: 添加 Registry.Listener 到 CompositeListenable
                    listeners.Add(registry.Listener);

                    // T031: 建立 RegistryConnectionManager (負責連接、斷線偵測與重連)
                    var log = PinionCore.Utility.Log.Instance;
                    registryConnectionManager = new RegistryConnectionManager(
                        registry,
                        options.RouterHost,
                        options.RouterPort.Value,
                        log
                    );

                    // 啟動連接管理器 (會自動建立連接)
                    registryConnectionManager.Start();

                    shutdownTasks.Add(() =>
                    {
                        registryWorkerDispose?.Invoke();
                        registryConnectionManager?.Dispose();
                        registry?.Dispose();
                    });

                    enabledModes.Add($"Gateway Router ({options.RouterHost}:{options.RouterPort}, Group {options.Group})");
                    System.Console.WriteLine($"[OK] Gateway mode initialized");
                }
                catch (Exception ex)
                {
                    // T052: Gateway 啟動失敗處理
                    System.Console.WriteLine($"[WARNING] Gateway mode failed to initialize: {ex.Message}");
                }
            }

            // T051: 顯示啟用的連線模式
            if (enabledModes.Count == 0)
            {
                System.Console.WriteLine("[ERROR] No connection modes enabled. Server will exit.");
                Environment.Exit(1);
                return;
            }

            System.Console.WriteLine("========================================");
            System.Console.WriteLine($"Chat Server Started - {enabledModes.Count} connection source(s) enabled:");
            foreach (var mode in enabledModes)
            {
                System.Console.WriteLine($"  - {mode}");
            }
            System.Console.WriteLine("========================================");

            var service = PinionCore.Remote.Server.Provider.CreateService(entry, protocol, listeners);
            IListenable listenable = listeners;

            // T082: 追蹤玩家連接統計
            int playerCount = 0;
            Action<PinionCore.Network.IStreamable> onPlayerConnected = (streamable) =>
            {
                playerCount++;
                System.Console.WriteLine($"[玩家連接] 當前玩家數: {playerCount}");
            };
            Action<PinionCore.Network.IStreamable> onPlayerDisconnected = (streamable) =>
            {
                playerCount--;
                System.Console.WriteLine($"[玩家斷線] 當前玩家數: {playerCount}");
            };

            listenable.StreamableEnterEvent += onPlayerConnected;
            listenable.StreamableLeaveEvent += onPlayerDisconnected;
            listenable.StreamableEnterEvent += service.Join;
            listenable.StreamableLeaveEvent += service.Leave;
            var console = new Console(entry.Announcement);
            console.Run(() =>
            {
                entry.Update();
                registryConnectionManager?.Update(); // 更新 RegistryConnectionManager 狀態機
            });

            // 優雅關閉
            foreach (var shutdown in shutdownTasks)
            {
                shutdown();
            }

            listenable.StreamableEnterEvent -= service.Join;
            listenable.StreamableLeaveEvent -= service.Leave;
            listenable.StreamableEnterEvent -= onPlayerConnected;
            listenable.StreamableLeaveEvent -= onPlayerDisconnected;
            service.Dispose();
        }
    }
}
