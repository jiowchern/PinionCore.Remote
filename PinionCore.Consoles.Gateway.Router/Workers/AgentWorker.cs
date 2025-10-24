using System;
using System.Threading;
using System.Threading.Tasks;

namespace PinionCore.Consoles.Gateway.Router.Workers
{
    /// <summary>
    /// AgentWorker 管理單一 Agent 的訊息循環生命週期
    /// 持續呼叫 HandlePackets() 與 HandleMessage() 維持通訊
    /// </summary>
    public class AgentWorker : IDisposable
    {
        private readonly PinionCore.Remote.Ghost.IAgent _agent;
        private readonly CancellationTokenSource _cts;
        private readonly Task _loopTask;

        public string Id { get; }
        public DateTime CreatedAt { get; }

        /// <summary>
        /// 當 Agent 發生錯誤時觸發，參數為錯誤例外
        /// </summary>
        public event Action<Exception>? ErrorEvent;

        public AgentWorker(PinionCore.Remote.Ghost.IAgent agent)
        {
            _agent = agent ?? throw new ArgumentNullException(nameof(agent));
            _cts = new CancellationTokenSource();
            Id = Guid.NewGuid().ToString("N").Substring(0, 8); // 簡短 ID
            CreatedAt = DateTime.UtcNow;

            // 啟動訊息循環
            _loopTask = Task.Run(() => MessageLoop(), _cts.Token);
        }

        private void MessageLoop()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    _agent.HandlePackets();
                    _agent.HandleMessage();
                }
                catch (Exception ex)
                {
                    // 觸發錯誤事件並中斷迴圈
                    ErrorEvent?.Invoke(ex);
                    break;
                }

                Thread.Sleep(1); // 避免 CPU 100%
            }
        }

        public void Dispose()
        {
            if (_cts == null) return;

            _cts.Cancel();

            try
            {
                _loopTask.Wait(TimeSpan.FromSeconds(5));
            }
            catch (AggregateException)
            {
                // 超時或任務已取消,繼續清理
            }

            _cts.Dispose();
        }
    }
}
