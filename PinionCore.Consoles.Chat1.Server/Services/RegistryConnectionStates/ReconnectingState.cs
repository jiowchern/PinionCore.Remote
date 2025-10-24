using System;
using System.Threading;
using System.Threading.Tasks;
using PinionCore.Utility;

namespace PinionCore.Consoles.Chat1.Server.Services.RegistryConnectionStates
{
    /// <summary>
    /// 重連狀態，使用指數退避算法
    /// </summary>
    internal class ReconnectingState : IStatus
    {
        private readonly PinionCore.Utility.Log _log;
        private readonly ExponentialBackoffReconnector _reconnector;
        private CancellationTokenSource _cts;
        private Task _reconnectTask;

        public event Action OnRetryConnect;

        public ReconnectingState(
            ExponentialBackoffReconnector reconnector,
            PinionCore.Utility.Log log)
        {
            _reconnector = reconnector;
            _log = log;
        }

        void IStatus.Enter()
        {
            _log.WriteInfo("Registry 狀態: 重連中");

            // 啟動重連流程
            _cts = new CancellationTokenSource();
            _reconnectTask = WaitAndRetryAsync(_cts.Token);
        }

        void IStatus.Update()
        {
            // 持續更新（重連邏輯在背景執行）
        }

        private async Task WaitAndRetryAsync(CancellationToken cancellationToken)
        {
            try
            {
                int delay = _reconnector.CalculateDelay();
                _log.WriteInfo(() => $"將在 {delay / 1000} 秒後重試");

                await Task.Delay(delay, cancellationToken);

                if (!cancellationToken.IsCancellationRequested)
                {
                    OnRetryConnect?.Invoke();
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            catch (Exception ex)
            {
                _log.WriteInfo(() => $"重連等待錯誤: {ex.Message}");
            }
        }

        void IStatus.Leave()
        {
            // 停止重連流程
            _cts?.Cancel();

            try
            {
                _reconnectTask?.Wait(TimeSpan.FromSeconds(2));
            }
            catch (AggregateException)
            {
                // 忽略任務取消異常
            }

            _cts?.Dispose();
        }
    }
}
