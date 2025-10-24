using System;
using System.Threading;
using System.Threading.Tasks;
using PinionCore.Remote.Gateway;

namespace PinionCore.Consoles.Chat1.Server.Services
{
    /// <summary>
    /// Registry Client 服務介面,管理 Chat Server 作為 Registry 連接到 Router
    /// </summary>
    public interface IRegistryClientService : IDisposable
    {
        /// <summary>
        /// Registry 實例 (暴露 Listener 給 Service 綁定)
        /// </summary>
        Registry Registry { get; }

        /// <summary>
        /// Router 主機位址
        /// </summary>
        string RouterHost { get; }

        /// <summary>
        /// Router Registry 端口
        /// </summary>
        int RouterPort { get; }

        /// <summary>
        /// 服務群組 ID
        /// </summary>
        uint Group { get; }

        /// <summary>
        /// 當前連線狀態
        /// </summary>
        RegistryConnectionState State { get; }

        /// <summary>
        /// 啟動服務並連接到 Router
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>連接任務</returns>
        Task StartAsync(CancellationToken cancellationToken);

        /// <summary>
        /// 停止服務並斷開連接
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>停止任務</returns>
        Task StopAsync(CancellationToken cancellationToken);

        /// <summary>
        /// 手動觸發重連
        /// </summary>
        /// <returns>重連任務</returns>
        Task ReconnectAsync();
    }

    /// <summary>
    /// Registry 連線狀態
    /// </summary>
    public enum RegistryConnectionState
    {
        /// <summary>未連接:初始狀態或連線失敗後</summary>
        Disconnected,

        /// <summary>連接中:正在嘗試連接到 Router</summary>
        Connecting,

        /// <summary>已連接:連線成功且註冊完成</summary>
        Connected,

        /// <summary>等待重連:斷線後進入指數退避重連</summary>
        WaitingRetry
    }
}
