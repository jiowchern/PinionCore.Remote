using System;
using System.Threading;
using System.Threading.Tasks;

namespace PinionCore.Consoles.Gateway.Router.Services
{
    /// <summary>
    /// Registry 監聽服務介面,管理 TCP 監聽器
    /// </summary>
    public interface IRegistryListenerService : IDisposable
    {
        /// <summary>
        /// Registry TCP 監聽端口
        /// </summary>
        int Port { get; }

        /// <summary>
        /// 是否已啟動
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// 啟動監聽服務
        /// </summary>
        /// <param name="routerService">關聯的 Router 服務</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>啟動任務</returns>
        /// <exception cref="InvalidOperationException">端口綁定失敗時拋出</exception>
        Task StartAsync(IRouterService routerService, CancellationToken cancellationToken);

        /// <summary>
        /// 停止監聽服務
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>停止任務</returns>
        Task StopAsync(CancellationToken cancellationToken);

        /// <summary>
        /// 取得當前已註冊的 Registry 連線數
        /// </summary>
        /// <returns>連線數</returns>
        int GetActiveRegistryCount();
    }
}
