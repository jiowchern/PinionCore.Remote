using System;
using System.Threading;
using System.Threading.Tasks;
using PinionCore.Remote.Gateway;
using PinionCore.Remote.Gateway.Hosts;

namespace PinionCore.Consoles.Gateway.Router.Services
{
    /// <summary>
    /// Router 服務介面,封裝 PinionCore.Remote.Gateway.Router 核心功能
    /// </summary>
    public interface IRouterService : IDisposable
    {
        /// <summary>
        /// Registry Client 連接端點
        /// </summary>
        IService RegistryEndpoint { get; }

        /// <summary>
        /// Agent 連接端點
        /// </summary>
        IService SessionEndpoint { get; }

        /// <summary>
        /// 負載平衡策略
        /// </summary>
        ISessionSelectionStrategy Strategy { get; }

        /// <summary>
        /// 啟動 Router 服務
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>啟動任務</returns>
        Task StartAsync(CancellationToken cancellationToken);

        /// <summary>
        /// 停止 Router 服務
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>停止任務</returns>
        Task StopAsync(CancellationToken cancellationToken);

        /// <summary>
        /// 取得當前狀態資訊 (用於日誌與監控)
        /// </summary>
        /// <returns>狀態描述字串</returns>
        string GetStatus();
    }
}
