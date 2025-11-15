using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using PinionCore.Remote.Server;
using PinionCore.Remote.Soul;
using PinionCore.Utility.WindowConsoleAppliction;
using PinionCore.Consoles.Chat1.Server.Configuration;
using PinionCore.Consoles.Chat1.Server.Services;

namespace PinionCore.Consoles.Chat1.Server
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            PinionCore.Utility.Log.Instance.RecordEvent += message => System.Console.WriteLine(message);
            // T084: �ˬd --help �� -h �Ѽ�
            if (args.Length > 0 && (args[0] == "--help" || args[0] == "-h" ||
                Array.Exists(args, arg => arg == "--help" || arg == "-h")))
            {
                System.Console.WriteLine(ChatServerOptions.GetUsageString());
                return;
            }

            // T029: �ѪR�R�O�C�Ѽ�
            var options = CommandLineParser.Parse(args);

            // ���ҰѼƦ��ĩ�
            if (!options.Validate(out var error))
            {
                System.Console.WriteLine($"���~: {error}");
                System.Console.WriteLine(ChatServerOptions.GetUsageString());
                Environment.Exit(1);
                return;
            }

            var protocol = PinionCore.Consoles.Chat1.Common.ProtocolCreator.Create();
            var entry = new PinionCore.Consoles.Chat1.Entry();
            var soul = new PinionCore.Remote.Server.Soul(entry, protocol);
            PinionCore.Remote.Soul.IService service = soul;

            var endpointDescriptors = new List<(PinionCore.Remote.Server.IListeningEndpoint Endpoint, string Description)>();
            var activePlayerSources = new List<PinionCore.Remote.Soul.IListenable>();
            var shutdownTasks = new List<Action>();
            var enabledModes = new List<string>();
            PinionCore.Remote.Soul.IListenable registryListener = null;
            var registryListenerAttached = false;

            // T046: ���s TCP ��ť��
            if (options.TcpPort.HasValue)
            {
                endpointDescriptors.Add((new PinionCore.Remote.Server.Tcp.ListeningEndpoint(options.TcpPort.Value, 10),
                    $"TCP (port {options.TcpPort.Value})"));
            }

            // T047: ���s WebSocket ��ť��
            if (options.WebPort.HasValue)
            {
                endpointDescriptors.Add((new PinionCore.Remote.Server.Web.ListeningEndpoint($"http://*:{options.WebPort.Value}/"),
                    $"WebSocket (port {options.WebPort.Value})"));
            }

            // T030: Registry Client ��l��
            PinionCore.Remote.Gateway.Registry registry = null;
            RegistryConnectionManager registryConnectionManager = null;
            Action registryWorkerDispose = null;

            // T048: Gateway ���Ѻ�ť����X
            if (options.HasGatewayMode)
            {
                try
                {
                    System.Console.WriteLine($"Initializing Gateway mode: Router {options.RouterHost}:{options.RouterPort} (Group: {options.Group})");

                    registry = new PinionCore.Remote.Gateway.Registry(protocol, options.Group);

                    // T032: �Ұ� AgentWorker (����B�z registry.Agent.HandlePackets/HandleMessage)
                    var agentWorkerRunning = true;
                    var agentWorkerTask = Task.Run(() =>
                    {
                        while (agentWorkerRunning)
                        {
                            registry.Agent.HandlePackets();
                            registry.Agent.HandleMessages();
                            System.Threading.Thread.Sleep(1); // �u�ȥ�v�קK������
                        }
                    });

                    registryWorkerDispose = () =>
                    {
                        agentWorkerRunning = false;
                        agentWorkerTask.Wait(TimeSpan.FromSeconds(2));
                    };

                    registryListener = registry.Listener;
                    registryListener.StreamableEnterEvent += service.Join;
                    registryListener.StreamableLeaveEvent += service.Leave;
                    activePlayerSources.Add(registryListener);
                    registryListenerAttached = true;

                    // T031: �إ� RegistryConnectionManager (�t�d�s���B�_�u�����P���s)
                    var log = PinionCore.Utility.Log.Instance;
                    registryConnectionManager = new RegistryConnectionManager(
                        registry,
                        options.RouterHost,
                        options.RouterPort.Value,
                        log
                    );

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
                    System.Console.WriteLine($"[WARNING] Gateway mode failed to initialize: {ex.Message}");
                }
            }

            var (listenHandle, errorInfos) = await service.ListenAsync(endpointDescriptors.Select(d => d.Endpoint).ToArray());
            var endpointErrorLookup = errorInfos.ToDictionary(info => info.ListeningEndpoint, info => info.Exception);

            foreach (var descriptor in endpointDescriptors)
            {
                if (endpointErrorLookup.TryGetValue(descriptor.Endpoint, out var exception))
                {
                    System.Console.WriteLine($"[WARNING] {descriptor.Description} listener failed to start: {exception?.Message}");
                }
                else
                {
                    enabledModes.Add(descriptor.Description);
                    activePlayerSources.Add(descriptor.Endpoint);
                    System.Console.WriteLine($"[OK] {descriptor.Description} listener started");
                }
            }

            // T051: ��ܱҥΪ��s�u�Ҧ�
            if (enabledModes.Count == 0)
            {
                System.Console.WriteLine("[ERROR] No connection modes enabled. Server will exit.");
                listenHandle.Dispose();
                soul.Dispose();
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

            // T082: �l�ܪ��a�s���έp
            int playerCount = 0;
            Action<PinionCore.Network.IStreamable> onPlayerConnected = (_) =>
            {
                playerCount++;
                System.Console.WriteLine($"[���a�s��] ���e���a��: {playerCount}");
            };
            Action<PinionCore.Network.IStreamable> onPlayerDisconnected = (_) =>
            {
                playerCount--;
                System.Console.WriteLine($"[���a�_�u] ���e���a��: {playerCount}");
            };

            foreach (var source in activePlayerSources)
            {
                source.StreamableEnterEvent += onPlayerConnected;
                source.StreamableLeaveEvent += onPlayerDisconnected;
            }

            var console = new Console(entry.Announcement);
            console.Run(() =>
            {
                entry.Update();
                registryConnectionManager?.Update(); // ��s RegistryConnectionManager ���A��
            });

            foreach (var shutdown in shutdownTasks)
            {
                shutdown();
            }

            foreach (var source in activePlayerSources)
            {
                source.StreamableEnterEvent -= onPlayerConnected;
                source.StreamableLeaveEvent -= onPlayerDisconnected;
            }

            if (registryListenerAttached && registryListener != null)
            {
                registryListener.StreamableEnterEvent -= service.Join;
                registryListener.StreamableLeaveEvent -= service.Leave;
            }

            listenHandle.Dispose();
            soul.Dispose();
        }
    }
}
