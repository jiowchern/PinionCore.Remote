using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using PinionCore.Remote;
using PinionCore.Remote.Client;

namespace PinionCore.Consoles.Chat1.Bots
{
    internal sealed class TcpApplication : IDisposable
    {
        private sealed class BotConnection
        {
            public required Bot Bot { get; init; }
            public required PinionCore.Remote.Ghost.IAgent Agent { get; init; }
            public required PinionCore.Remote.Client.Tcp.ConnectingEndpoint Endpoint { get; init; }
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
            var ghost = new PinionCore.Remote.Client.Proxy(_protocol);
            var agent = ghost.Agent;
            var endpoint = new PinionCore.Remote.Client.Tcp.ConnectingEndpoint(_endPoint);
            PinionCore.Remote.Client.IConnectingEndpoint connectable = endpoint;
            var stream = await connectable.ConnectAsync().ConfigureAwait(false);

            agent.Enable(stream);
            var bot = new Bot(agent);

            void HandleBreak()
            {
                System.Console.WriteLine($"Peer disconnected: {_endPoint}");
                RemoveBot(bot);
            }

            endpoint.BreakEvent += HandleBreak;

            lock (_bots)
            {
                _bots.Add(new BotConnection
                {
                    Bot = bot,
                    Agent = agent,
                    Endpoint = endpoint,
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

            target.Endpoint.BreakEvent -= target.BreakHandler;
            target.Bot.Dispose();
            target.Agent.Disable();
            IDisposable disposable = target.Endpoint;
            disposable.Dispose();
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
                connection.Endpoint.BreakEvent -= connection.BreakHandler;
                connection.Bot.Dispose();
                connection.Agent.Disable();
                IDisposable disposable = connection.Endpoint;
                disposable.Dispose();
            }
        }
    }
}
