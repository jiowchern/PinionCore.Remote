

namespace PinionCore.Consoles.Gateway.Router.Infrastructure
{
    /// <summary>
    /// 處理 SIGTERM/SIGINT 訊號，執行優雅關閉流程
    /// 20 秒超時保護
    /// </summary>
    public class GracefulShutdownHandler
    {
        private readonly CancellationTokenSource _shutdownCts;
        private readonly TimeSpan _timeout;

        /// <summary>
        /// 關閉令牌，當接收到關閉訊號時觸發
        /// </summary>
        public CancellationToken ShutdownToken => _shutdownCts.Token;

        public GracefulShutdownHandler(TimeSpan timeout)
        {
            _timeout = timeout;
            _shutdownCts = new CancellationTokenSource();
        }

        /// <summary>
        /// 註冊訊號處理器 (SIGTERM/SIGINT)
        /// </summary>
        public void Register(PinionCore.Utility.Log log)
        {
            // 捕捉 Ctrl+C (SIGINT)
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;  // 防止立即終止
                log.WriteInfo("收到 SIGINT 訊號，開始優雅關閉...");
                _shutdownCts.Cancel();
            };

            // 捕捉 Process Exit (SIGTERM in Docker)
            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                log.WriteInfo("收到 SIGTERM 訊號，開始優雅關閉...");
                _shutdownCts.Cancel();
            };
        }

        /// <summary>
        /// 執行優雅關閉流程
        /// </summary>
        public async Task ExecuteShutdownAsync(Func<CancellationToken, Task> shutdownAction, PinionCore.Utility.Log log)
        {
            var shutdownCts = new CancellationTokenSource(_timeout);
            var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(_shutdownCts.Token, shutdownCts.Token);

            try
            {
                log.WriteInfo($"開始優雅關閉流程 (超時時間: {_timeout.TotalSeconds} 秒)");
                await shutdownAction(combinedCts.Token);
                log.WriteInfo("優雅關閉完成");
            }
            catch (OperationCanceledException)
            {
                log.WriteInfo("[WARNING] " + $"優雅關閉超時 ({_timeout.TotalSeconds} 秒)，強制終止");
            }
            catch (Exception ex)
            {
                log.WriteInfo("[ERROR] " + $"優雅關閉發生錯誤: {ex.Message}");
            }
            finally
            {
                combinedCts.Dispose();
                shutdownCts.Dispose();
            }
        }
    }
}
