using System;
using System.Net;
using System.Threading.Tasks;
using PinionCore.Consoles.Chat1.Common;

namespace PinionCore.Consoles.Chat1.Bots
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length < 3)
            {
                System.Console.WriteLine("Usage: dotnet run -- [address] [port] [botCount] [mode: tcp|standalone]");
                return;
            }

            var address = args[0];
            if (!int.TryParse(args[1], out var port))
            {
                System.Console.WriteLine("Invalid port.");
                return;
            }

            if (!int.TryParse(args[2], out var botCount) || botCount <= 0)
            {
                System.Console.WriteLine("Invalid bot count.");
                return;
            }

            var mode = args.Length >= 4 ? args[3] : "tcp";

            var protocol = ProtocolCreator.Create();

            if (string.Equals(mode, "standalone", StringComparison.OrdinalIgnoreCase))
            {
                using var app = new StandaloneApplication(protocol, botCount);
                app.Run();
            }
            else
            {
                var endPoint = new IPEndPoint(IPAddress.Parse(address), port);
                using var app = new TcpApplication(protocol, botCount, endPoint);
                await app.Run().ConfigureAwait(false);
            }
        }
    }
}
