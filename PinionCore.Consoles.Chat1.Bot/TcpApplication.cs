using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using PinionCore.Remote;

namespace PinionCore.Consoles.Chat1.Bots
{
    internal sealed class TcpApplication : IDisposable
    {
        private sealed class BotConnection
        {
            public required Bot Bot { get; init; }
            public required PinionCore.Remote.Client.TcpConnectSet Set { get; init; }
            public required PinionCore.Network.Tcp.Peer Peer { get; init; }
            public required System.Action BreakHandler { get; init; }
        }

        private readonly List<BotConnection> _bots;
        private readonly IProtocol _protocol;
        private readonly int _botCount;
        private readonly IPEndPoint _endPoint;

        public TcpApplication(IProtocol protocol, int botCount, IPEndPoint endPoint)
        {
            _protocol = protocol;
            _botCount = botCount;
            _endPoint = endPoint;
            _bots = new List<BotConnection>();
        }

        public async Task Run()
        {
            for (var i = 0; i < _botCount; i++)
            {
                await AddBotAsync().ConfigureAwait(false);
            }

            System.Console.WriteLine($"TCP bots running: {_botCount}. Press Enter to stop.");
            System.Console.ReadLine();
        }

        private async Task AddBotAsync()
        {
            var set = PinionCore.Remote.Client.Provider.CreateTcpAgent(_protocol);
            var peer = await set.Connector.Connect(_endPoint).ConfigureAwait(false);
            if (peer == null)
            {
                System.Console.WriteLine($"Failed to connect to {_endPoint}.");
                return;
            }

            set.Agent.Enable(peer);
            var bot = new Bot(set.Agent);

            void HandleBreak()
            {
                System.Console.WriteLine($"Peer disconnected: {_endPoint}");
                RemoveBot(bot);
            }

            peer.BreakEvent += HandleBreak;

            lock (_bots)
            {
                _bots.Add(new BotConnection
                {
                    Bot = bot,
                    Set = set,
                    Peer = peer,
                    BreakHandler = HandleBreak
                });
            }
        }

        private void RemoveBot(Bot bot)
        {
            BotConnection? target = null;
            lock (_bots)
            {
                target = _bots.Find(connection => ReferenceEquals(connection.Bot, bot));
                if (target != null)
                {
                    _bots.Remove(target);
                }
            }

            if (target == null)
            {
                return;
            }

            target.Peer.BreakEvent -= target.BreakHandler;
            target.Bot.Dispose();
            target.Set.Agent.Disable();
            target.Set.Connector.Disconnect().GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            List<BotConnection> snapshot;
            lock (_bots)
            {
                snapshot = new List<BotConnection>(_bots);
                _bots.Clear();
            }

            foreach (var connection in snapshot)
            {
                connection.Peer.BreakEvent -= connection.BreakHandler;
                connection.Bot.Dispose();
                connection.Set.Agent.Disable();
                connection.Set.Connector.Disconnect().GetAwaiter().GetResult();
            }
        }
    }
}

