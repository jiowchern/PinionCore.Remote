using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using PinionCore.Remote.Soul;
using PinionCore.Utility.WindowConsoleAppliction;

namespace PinionCore.Consoles.Chat1.Server
{
    internal static class Program
    {
        internal static int Main(string[] args)
        {
            var tcpOption = new Option<int?>("-tcp", new[] { "--tcpport" })
            {
                Description = "TCP listener port."
            };

            var webOption = new Option<int?>("-web", new[] { "--webport" })
            {
                Description = "Web listener port."
            };

            var gateOption = new Option<int?>("-gate", new[] { "--gateway" })
            {
                Description = "Web listener port."
            };

            var portsArgument = new Argument<List<int>>("ports")
            {
                Arity = ArgumentArity.ZeroOrMore,
                Description = "Fallback positional ports: <tcp> [web]",
                DefaultValueFactory = _ => new List<int>()
            };

            var rootCommand = new RootCommand("PinionCore Chat1 server host");
            rootCommand.Add(tcpOption);
            rootCommand.Add(gateOption);
            rootCommand.Add(webOption);
            rootCommand.Add(portsArgument);

            var parseResult = CommandLineParser.Parse(rootCommand, args, new ParserConfiguration());
            if (parseResult.Errors.Count > 0)
            {
                foreach (var error in parseResult.Errors)
                {
                    System.Console.Error.WriteLine(error.Message);
                }

                return 1;
            }

            var tcpPort = parseResult.GetValue(tcpOption) ?? 0;
            var webPort = parseResult.GetValue(webOption) ?? 0;
            var gatewayPort = parseResult.GetValue(gateOption) ?? 0;
            var positionalPorts = parseResult.GetValue(portsArgument) ?? new List<int>();
            var (effectiveTcp, effectiveWeb) = ResolvePorts(tcpPort, webPort, positionalPorts);

            RunServer(effectiveTcp, effectiveWeb, gatewayPort);
            return 0;
        }

        private static (int tcpPort, int webPort) ResolvePorts(int tcpPort, int webPort, List<int> positional)
        {
            if (tcpPort == 0 && positional.Count > 0)
            {
                tcpPort = positional[0];
            }

            if (webPort == 0 && positional.Count > 1)
            {
                webPort = positional[1];
            }

            return (tcpPort, webPort);
        }

        private static void RunServer(int tcpPort, int webPort,int gateway)
        {
            var protocol = PinionCore.Consoles.Chat1.Common.ProtocolCreator.Create();
            var entry = new PinionCore.Consoles.Chat1.Entry();

            var listeners = new CompositeListenable();
            var shutdownTasks = new List<Action>();

            if (gateway != 0)
            {

                var tcp = new PinionCore.Remote.Server.Tcp.Listener();
                IListenable listener = tcp;
                var gateListener = new PinionCore.Remote.Gateway.Servers.GatewayServerServiceHub();

                listener.StreamableLeaveEvent += gateListener.Source.Leave;
                listener.StreamableEnterEvent += gateListener.Source.Join;
                listeners.Add(gateListener.Sink);
                tcp.Bind(gateway);
                shutdownTasks.Add(() => gateListener.Dispose());
                shutdownTasks.Add(() => tcp.Close());
            }

            if (tcpPort != 0)
            {
                var tcp = new PinionCore.Remote.Server.Tcp.Listener();
                listeners.Add(tcp);
                tcp.Bind(tcpPort);
                shutdownTasks.Add(() => tcp.Close());
                System.Console.WriteLine($"TCP listener started on port {tcpPort}.");
            }

            if (webPort != 0)
            {
                var web = new PinionCore.Remote.Server.Web.Listener();
                listeners.Add(web);
                web.Bind($"http://*:{webPort}/");
                shutdownTasks.Add(() => web.Close());
                System.Console.WriteLine($"Web listener started on port {webPort}.");
            }

            var service = PinionCore.Remote.Server.Provider.CreateService(entry, protocol, listeners);
            IListenable listenable = listeners;
            listenable.StreamableEnterEvent += service.Join;
            listenable.StreamableLeaveEvent += service.Leave;
            var console = new Console(entry.Announcement);
            console.Run(() => entry.Update());

            foreach (var shutdown in shutdownTasks)
            {
                shutdown();
            }

            listenable.StreamableEnterEvent -= service.Join;
            listenable.StreamableLeaveEvent -= service.Leave;
            service.Dispose();
        }
    }
}
