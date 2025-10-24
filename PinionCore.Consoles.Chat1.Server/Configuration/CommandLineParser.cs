using System;

namespace PinionCore.Consoles.Chat1.Server.Configuration
{
    /// <summary>
    /// 命令列參數解析器
    /// </summary>
    public static class CommandLineParser
    {
        /// <summary>
        /// 解析命令列參數到 ChatServerOptions
        /// </summary>
        public static ChatServerOptions Parse(string[] args)
        {
            var options = new ChatServerOptions();

            if (args == null || args.Length == 0)
            {
                return options;
            }

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                // --tcp-port=PORT 或 --tcpport=PORT
                if (TryParseNamedInt(arg, new[] { "--tcp-port", "--tcpport", "--tcp" }, out var tcpPort))
                {
                    options.TcpPort = tcpPort;
                    continue;
                }

                // --web-port=PORT 或 --webport=PORT
                if (TryParseNamedInt(arg, new[] { "--web-port", "--webport", "--web" }, out var webPort))
                {
                    options.WebPort = webPort;
                    continue;
                }

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

                // --group=ID
                if (TryParseNamedUInt(arg, new[] { "--group" }, out var group))
                {
                    options.Group = group;
                    continue;
                }

                // --help
                if (arg.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("-h", StringComparison.OrdinalIgnoreCase))
                {
                    System.Console.WriteLine(ChatServerOptions.GetUsageString());
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
                // 支援 --name=value 格式
                if (arg.StartsWith($"{name}=", StringComparison.OrdinalIgnoreCase))
                {
                    var valueStr = arg.Substring(name.Length + 1);
                    return int.TryParse(valueStr, out value);
                }

                // 支援 --name (忽略,等待下一個參數)
                if (arg.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return false; // 需要從下一個參數讀取
                }
            }

            return false;
        }

        private static bool TryParseNamedString(string arg, string[] names, out string value)
        {
            value = null;

            foreach (var name in names)
            {
                // 支援 --name=value 格式
                if (arg.StartsWith($"{name}=", StringComparison.OrdinalIgnoreCase))
                {
                    value = arg.Substring(name.Length + 1);
                    return !string.IsNullOrWhiteSpace(value);
                }
            }

            return false;
        }

        private static bool TryParseNamedUInt(string arg, string[] names, out uint value)
        {
            value = 0;

            foreach (var name in names)
            {
                // 支援 --name=value 格式
                if (arg.StartsWith($"{name}=", StringComparison.OrdinalIgnoreCase))
                {
                    var valueStr = arg.Substring(name.Length + 1);
                    return uint.TryParse(valueStr, out value);
                }
            }

            return false;
        }
    }
}
