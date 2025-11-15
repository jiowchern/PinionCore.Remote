using System;
using System.Collections.Generic;
using PinionCore.Remote;
using PinionCore.Remote.Client;
using PinionCore.Remote.Server;

namespace PinionCore.Consoles.Chat1.Bots
{
    internal sealed class StandaloneApplication : IDisposable
    {
        private readonly IProtocol _protocol;
        private readonly int _botCount;
        private readonly Entry _entry;
        private readonly PinionCore.Remote.Server.Soul _soul;
        private readonly PinionCore.Remote.Standalone.ListeningEndpoint _endpoint;
        private readonly List<(Bot bot, PinionCore.Remote.Ghost.IAgent agent)> _bots;
        private IDisposable? _listenHandle;

        public StandaloneApplication(IProtocol protocol, int botCount)
        {
            _protocol = protocol;
            _botCount = botCount;
            _entry = new Entry();
            _soul = new PinionCore.Remote.Server.Soul(_entry, _protocol);
            _endpoint = new PinionCore.Remote.Standalone.ListeningEndpoint();
            _bots = new List<(Bot, PinionCore.Remote.Ghost.IAgent)>();
        }

        public void Run()
        {
            if (_listenHandle == null)
            {
                PinionCore.Remote.Soul.IService service = _soul;
                var (handle, errorInfos) = service.ListenAsync(_endpoint).GetAwaiter().GetResult();
                foreach (var error in errorInfos)
                {
                    throw new InvalidOperationException($"Standalone listener error: {error.Exception}");
                }

                _listenHandle = handle;
            }

            for (var i = 0; i < _botCount; i++)
            {
                var agent = new PinionCore.Remote.Ghost.User(_protocol);
                var connectable = (PinionCore.Remote.Client.IConnectingEndpoint)_endpoint;
                var stream = connectable.ConnectAsync().GetAwaiter().GetResult();
                agent.Enable(stream);
                var bot = new Bot(agent);
                _bots.Add((bot, agent));
            }

            System.Console.WriteLine($"Standalone bots running: {_botCount}. Press Enter to stop.");
            System.Console.ReadLine();
        }

        public void Dispose()
        {
            foreach (var (bot, agent) in _bots)
            {
                bot.Dispose();
                agent.Disable();
            }
            _bots.Clear();
            _listenHandle?.Dispose();
            ((IDisposable)_endpoint).Dispose();
            _soul.Dispose();
        }
    }
}
