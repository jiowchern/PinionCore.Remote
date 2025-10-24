using PinionCore.Remote.Gateway;
using PinionCore.Remote.Gateway.Hosts;
using PinionCore.Utility;

namespace PinionCore.Consoles.Gateway.Router.Services
{
    /// <summary>
    /// 封裝 Gateway.Router 實例，提供核心路由服務
    /// 使用事件驅動架構，無需手動 Update 迴圈
    /// </summary>
    public class RouterService : IDisposable
    {
        private readonly PinionCore.Remote.Gateway.Router _router;
        private readonly Log _log;
        private bool _disposed = false;

        public RouterService(Log log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));

            // 使用 Round-Robin 負載平衡策略
            var strategy = new RoundRobinSelector();
            _router = new PinionCore.Remote.Gateway.Router(strategy);

            _log.WriteInfo("Router 啟動成功，負載平衡策略: Round-Robin");
        }

        /// <summary>
        /// Registry 端點 (供遊戲服務連接)
        /// </summary>
        public PinionCore.Remote.Soul.IService RegistryEndpoint => _router.Registry;

        /// <summary>
        /// Session 端點 (供客戶端 Agent 連接)
        /// </summary>
        public PinionCore.Remote.Soul.IService SessionEndpoint => _router.Session;

        public void Dispose()
        {
            if (_disposed)
                return;

            _log.WriteInfo("關閉 Router 服務...");

            try
            {
                _router?.Dispose();
            }
            catch (Exception ex)
            {
                _log.WriteInfo(() => $"關閉 Router 時發生錯誤: {ex.Message}");
            }

            _disposed = true;
        }
    }
}
