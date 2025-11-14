
using System;
using System.Threading;
using System.Threading.Tasks;
using PinionCore.Remote.Ghost;


namespace PinionCore.Remote.Gateway.Tests
{

    public sealed class AgentWorker : IDisposable
    {
        private readonly IAgent _agent;
        private readonly TimeSpan _sleepInterval;
        private CancellationTokenSource? _cts;
        private Task? _loopTask;

        public AgentWorker(IAgent agent, TimeSpan? sleepInterval = null)
        {
            _agent = agent ?? throw new ArgumentNullException(nameof(agent));
            _sleepInterval = sleepInterval ?? TimeSpan.FromMilliseconds(1);

            Start();
        }

        public bool IsRunning => _loopTask is { IsCompleted: false };

        void Start()
        {
            if (IsRunning)
            {
                return;
            }

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _loopTask = Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    var ping = _agent.Ping;
                    _agent.HandlePackets();
                    _agent.HandleMessages();
                    if (_sleepInterval > TimeSpan.Zero)
                    {
                        Thread.Sleep(_sleepInterval);
                    }
                }
            }, token);
        }

        async Task StopAsync()
        {
            if (_cts == null || _loopTask == null)
            {
                return;
            }

            _cts.Cancel();
            try
            {
                await _loopTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // 預期的取消例外
            }

            _cts.Dispose();
            _cts = null;
            _loopTask = null;
        }

        public async void Dispose()
        {
            await StopAsync();
        }
    }
}
