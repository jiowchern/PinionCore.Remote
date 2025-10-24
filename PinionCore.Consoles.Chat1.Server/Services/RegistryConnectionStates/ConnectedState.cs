using System;
using System.Threading;
using System.Threading.Tasks;
using PinionCore.Utility;

namespace PinionCore.Consoles.Chat1.Server.Services.RegistryConnectionStates
{
    /// <summary>
    /// 已連接狀態，持續監控連線
    /// </summary>
    internal class ConnectedState : IStatus
    {
        private readonly PinionCore.Remote.Gateway.Registry _registry;
        private readonly PinionCore.Utility.Log _log;
        private CancellationTokenSource _monitorCts;
        private Task _monitorTask;

        public event Action OnDisconnected;

        public ConnectedState(
            PinionCore.Remote.Gateway.Registry registry,
            PinionCore.Utility.Log log)
        {
            _registry = registry;
            _log = log;
        }

        void IStatus.Enter()
        {
            _log.WriteInfo("Registry 狀態: 已連接");

            // 啟動斷線監控
            _monitorCts = new CancellationTokenSource();
            _monitorTask = MonitorConnectionAsync(_monitorCts.Token);
        }

        void IStatus.Update()
        {
            // 持續更新（如有需要可添加邏輯）
        }

        private async Task MonitorConnectionAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (_registry.Agent.Ping <= 0)  // 偵測斷線 (Ping 為 0 或負數表示斷線)
                    {
                        _log.WriteInfo("偵測到 Router 連線中斷");
                        OnDisconnected?.Invoke();
                        break;
                    }

                    await Task.Delay(1000, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            catch (Exception ex)
            {
                _log.WriteInfo(() => $"斷線監控錯誤: {ex.Message}");
            }
        }

        void IStatus.Leave()
        {
            // 停止監控
            _monitorCts?.Cancel();

            try
            {
                _monitorTask?.Wait(TimeSpan.FromSeconds(2));
            }
            catch (AggregateException)
            {
                // 忽略任務取消異常
            }

            _monitorCts?.Dispose();
        }
    }
}
