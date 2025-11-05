using System;
using System.Collections.Generic;
using PinionCore.Remote;

namespace PinionCore.Consoles.Chat1.Bots
{
    internal sealed class StandaloneApplication : IDisposable
    {
        private readonly IProtocol _protocol;
        private readonly int _botCount;
        private readonly Entry _entry;
        private readonly PinionCore.Remote.Soul.Service _service;
        private readonly List<(Bot bot, Action disconnect)> _bots;

        public StandaloneApplication(IProtocol protocol, int botCount)
        {
            _protocol = protocol;
            _botCount = botCount;
            _entry = new Entry();
            _service = new PinionCore.Remote.Soul.Service(_entry, _protocol);
            _bots = new List<(Bot, Action)>();
        }

        public void Run()
        {
            for (var i = 0; i < _botCount; i++)
            {
                var agent = new PinionCore.Remote.Ghost.Agent(_protocol);
                var disconnect = PinionCore.Remote.Standalone.Provider.Connect(agent, _service);
                var bot = new Bot(agent);
                _bots.Add((bot, disconnect));
            }

            System.Console.WriteLine($"Standalone bots running: {_botCount}. Press Enter to stop.");
            System.Console.ReadLine();
        }

        public void Dispose()
        {
            foreach (var (bot, disconnect) in _bots)
            {
                bot.Dispose();
                disconnect();
            }
            _bots.Clear();
            _service.Dispose();
        }
    }
}

