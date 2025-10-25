using System;
using System.Threading;
using System.Threading.Tasks;
using PinionCore.Utility;
using PinionCore.Consoles.Gateway.Router.Infrastructure;

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
        private readonly Log _log;

        public string Id { get; }
        public DateTime CreatedAt { get; }

        /// <summary>
        /// 當 Agent 發生錯誤時觸發，參數為錯誤例外
        /// </summary>
        public event Action<Exception>? ErrorEvent;

        public AgentWorker(PinionCore.Remote.Ghost.IAgent agent, Log log = null)
        {
            _agent = agent ?? throw new ArgumentNullException(nameof(agent));
            _log = log ?? Log.Instance; // 使用提供的 log 或預設實例
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
                    // T083: 記錄詳細錯誤資訊到日誌
                    ErrorHandler.LogError(_log, $"Agent Worker [{Id}] 訊息處理錯誤", ex);

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
            catch (AggregateException ex)
            {
                // T083: 記錄超時或取消錯誤
                ErrorHandler.LogWarning(_log, $"Agent Worker [{Id}] 關閉超時或已取消", ex);
            }

            _cts.Dispose();
        }
    }
}
