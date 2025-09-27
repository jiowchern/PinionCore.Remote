using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using PinionCore.Consoles.Chat1.Common;
using PinionCore.Utility.WindowConsoleAppliction;

namespace PinionCore.Consoles.Chat1.Client
{
    internal static class Program
    {
        private const string StandaloneSwitch = "--standalone";

        static void Main(string[] args)
        {
            if (args.Length >= 2 && string.Equals(args[0], StandaloneSwitch, StringComparison.OrdinalIgnoreCase))
            {
                RunStandalone(new FileInfo(args[1]));
                return;
            }

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
