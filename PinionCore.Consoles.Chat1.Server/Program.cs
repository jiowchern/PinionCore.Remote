using System;
using System.Collections.Generic;
using PinionCore.Remote.Soul;
using PinionCore.Utility.WindowConsoleAppliction;

namespace PinionCore.Consoles.Chat1.Server
{
    internal static class Program
    {
        private static readonly string[] TcpSwitches = { "--tcp", "--tcpport" };
        private static readonly string[] WebSwitches = { "--web", "--webport" };

        static void Main(string[] args)
        {
            var (tcpPort, webPort) = ParsePorts(args);

            var protocol = PinionCore.Consoles.Chat1.Common.ProtocolCreator.Create();
            var entry = new PinionCore.Consoles.Chat1.Entry();

            var listeners = new CompositeListenable();
            var shutdownTasks = new List<Action>();

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

        private static (int tcpPort, int webPort) ParsePorts(string[] args)
        {
            var tcpPort = 0;
            var webPort = 0;

            if (args == null || args.Length == 0)
            {
                return (tcpPort, webPort);
            }

            var numericArgs = new System.Collections.Generic.List<int>();

            for (var i = 0; i < args.Length; i++)
            {
                var current = args[i];
                if (MatchesSwitch(current, TcpSwitches) && TryReadPort(args, ref i, out var parsedTcp))
                {
                    tcpPort = parsedTcp;
                    continue;
                }

                if (MatchesSwitch(current, WebSwitches) && TryReadPort(args, ref i, out var parsedWeb))
                {
                    webPort = parsedWeb;
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(current) && !current.StartsWith("-", StringComparison.Ordinal) && int.TryParse(current, out var numeric))
                {
                    numericArgs.Add(numeric);
                }
            }

            if (tcpPort == 0 && numericArgs.Count > 0)
            {
                tcpPort = numericArgs[0];
            }

            if (webPort == 0 && numericArgs.Count > 1)
            {
                webPort = numericArgs[1];
            }

            return (tcpPort, webPort);
        }

        private static bool TryReadPort(string[] args, ref int index, out int port)
        {
            port = 0;
            if (index + 1 >= args.Length)
            {
                return false;
            }

            if (int.TryParse(args[index + 1], out port))
            {
                index++;
                return true;
            }

            return false;
        }

        private static bool MatchesSwitch(string value, string[] candidates)
        {
            foreach (var candidate in candidates)
            {
                if (string.Equals(value, candidate, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

