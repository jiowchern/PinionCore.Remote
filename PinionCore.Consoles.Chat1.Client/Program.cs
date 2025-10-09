using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Reflection;
using PinionCore.Consoles.Chat1.Common;
using PinionCore.Utility.WindowConsoleAppliction;

namespace PinionCore.Consoles.Chat1.Client
{
    internal static class Program
    {
        internal static int Main(string[] args)
        {
            var standaloneOption = new Option<FileInfo?>("--standalone")
            {
                Description = "Run in standalone mode with the specified service assembly.",
                HelpName = "service.dll"
            };

            var hostArgument = new Argument<string?>("host")
            {
                Arity = ArgumentArity.ZeroOrOne,
                Description = "Remote host name."
            };

            var portArgument = new Argument<int?>("port")
            {
                Arity = ArgumentArity.ZeroOrOne,
                Description = "Remote host port.",
                DefaultValueFactory = _ => null
            };

            var rootCommand = new RootCommand("PinionCore Chat1 client");
            rootCommand.Add(standaloneOption);
            rootCommand.Add(hostArgument);
            rootCommand.Add(portArgument);

            var parseResult = CommandLineParser.Parse(rootCommand, args, new ParserConfiguration());
            if (parseResult.Errors.Count > 0)
            {
                foreach (var error in parseResult.Errors)
                {
                    System.Console.Error.WriteLine(error.Message);
                }

                return 1;
            }

            var standaloneAssembly = parseResult.GetValue(standaloneOption);
            var host = parseResult.GetValue(hostArgument);
            var port = parseResult.GetValue(portArgument);

            if (standaloneAssembly != null)
            {
                RunStandalone(standaloneAssembly);
                return 0;
            }

            RunRemote(host, port);
            return 0;
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

        private static void RunRemote(string? host, int? port)
        {
            System.Console.WriteLine("Remote mode.");
            var protocol = ProtocolCreator.Create();
            var set = PinionCore.Remote.Client.Provider.CreateTcpAgent(protocol);
            var console = new RemoteConsole(set);

            if (!string.IsNullOrWhiteSpace(host) && port.HasValue)
            {
                console.Connect(host, port.Value);
            }

            console.Run();
        }
    }
}
