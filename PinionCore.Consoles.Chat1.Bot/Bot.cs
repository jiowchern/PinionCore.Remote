using System;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using PinionCore.Consoles.Chat1.Common;
using PinionCore.Remote.Ghost;
using PinionCore.Remote.Reactive;

namespace PinionCore.Consoles.Chat1.Bots
{
    internal sealed class Bot : IDisposable
    {
        private readonly IAgent _agent;
        private readonly ConcurrentQueue<Action> _actions;
        private readonly System.Reactive.Disposables.CompositeDisposable _subscriptions;
        private readonly CancellationTokenSource _cancel;
        private readonly Task _loop;

        public Bot(IAgent agent)
        {
            _agent = agent;
            _actions = new ConcurrentQueue<Action>();
            _subscriptions = new System.Reactive.Disposables.CompositeDisposable();
            _cancel = new CancellationTokenSource();

            _loop = Task.Run(UpdateLoopAsync, _cancel.Token);
        }

        private async Task UpdateLoopAsync()
        {
            var random = new Random();

            var loginFlow = from login in _agent.QueryNotifier<ILogin>().SupplyEvent()
                            let name = $"bot-{_agent.GetHashCode()}-{login.GetHashCode()}"
                            from loginResult in login.Login(name).RemoteValue()
                            select loginResult;
            _subscriptions.Add(loginFlow.Subscribe(_ => Log($"[{_agent.GetHashCode()}] login success.")));

            var sendFlow = from player in _agent.QueryNotifier<IPlayer>().SupplyEvent()
                           from delay in Observable.Timer(TimeSpan.FromSeconds(random.Next(1, 4)))
                           select player;
            _subscriptions.Add(sendFlow.Subscribe(player => Enqueue(() => player.Send(DateTime.Now.ToString()))));

            var quitFlow = from player in _agent.QueryNotifier<IPlayer>().SupplyEvent()
                           from delay in Observable.Timer(TimeSpan.FromSeconds(random.Next(5, 10)))
                           select player;
            _subscriptions.Add(quitFlow.Subscribe(player => Enqueue(player.Quit)));

            var messageFlow = from player in _agent.QueryNotifier<IPlayer>().SupplyEvent()
                              from @event in Observable.FromEvent<Action<Message>, Message>(
                                  handler => player.PublicMessageEvent += handler,
                                  handler => player.PublicMessageEvent -= handler)
                              select @event;
            _subscriptions.Add(messageFlow.Subscribe(msg => Log($"{_agent.GetHashCode()}[{msg.Name}]: {msg.Context}")));

            try
            {
                while (!_cancel.IsCancellationRequested)
                {
                    _agent.HandleMessage();
                    _agent.HandlePackets();

                    while (_actions.TryDequeue(out var action))
                    {
                        action();
                    }

                    await Task.Delay(10, _cancel.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void Enqueue(Action action)
        {
            _actions.Enqueue(action);
        }

        private static void Log(string message)
        {
            PinionCore.Utility.Log.Instance.WriteInfo(message);
        }

        public void Dispose()
        {
            _cancel.Cancel();
            try
            {
                _loop.Wait();
            }
            catch (AggregateException ex) when (ex.InnerException is OperationCanceledException)
            {
            }
            _subscriptions.Dispose();
            _cancel.Dispose();
        }
    }
}
