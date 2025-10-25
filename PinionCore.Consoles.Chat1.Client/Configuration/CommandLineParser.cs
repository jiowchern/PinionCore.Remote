using System;

namespace PinionCore.Consoles.Chat1.Client.Configuration
{
    /// <summary>
    /// }��x�h
    /// </summary>
    public static class CommandLineParser
    {
        /// <summary>
        /// �}��x0 ChatClientOptions
        /// </summary>
        public static ChatClientOptions Parse(string[] args)
        {
            var options = new ChatClientOptions();

            if (args == null || args.Length == 0)
            {
                return options;
            }

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                // --router-host=HOST
                if (TryParseNamedString(arg, new[] { "--router-host", "--routerhost" }, out var routerHost))
                {
                    options.RouterHost = routerHost;
                    continue;
                }

                // --router-port=PORT
                if (TryParseNamedInt(arg, new[] { "--router-port", "--routerport" }, out var routerPort))
                {
                    options.RouterPort = routerPort;
                    continue;
                }

                // --websocket
                if (arg.Equals("--websocket", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("--ws", StringComparison.OrdinalIgnoreCase))
                {
                    options.UseWebSocket = true;
                    continue;
                }

                // --help
                if (arg.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("-h", StringComparison.OrdinalIgnoreCase))
                {
                    System.Console.WriteLine(ChatClientOptions.GetUsageString());
                    Environment.Exit(0);
                }
            }

            return options;
        }

        private static bool TryParseNamedInt(string arg, string[] names, out int value)
        {
            value = 0;

            foreach (var name in names)
            {
                // /� --name=value <
                if (arg.StartsWith($"{name}=", StringComparison.OrdinalIgnoreCase))
                {
                    var valueStr = arg.Substring(name.Length + 1);
                    return int.TryParse(valueStr, out value);
                }
            }

            return false;
        }

        private static bool TryParseNamedString(string arg, string[] names, out string value)
        {
            value = null;

            foreach (var name in names)
            {
                // /� --name=value <
                if (arg.StartsWith($"{name}=", StringComparison.OrdinalIgnoreCase))
                {
                    value = arg.Substring(name.Length + 1);
                    return !string.IsNullOrWhiteSpace(value);
                }
            }

            return false;
        }
    }
}
