using PinionCore.Network;
using PinionCore.Remote.Server;
using PinionCore.Remote.Soul;
using PinionCore.Utility;
using Tcp = PinionCore.Remote.Server.Tcp;

namespace PinionCore.Consoles.Gateway.Router.Services
{
    /// <summary>
    /// 管理 Registry TCP 監聽器
    /// 使用事件驅動模式整合到 Router.Registry 端點
    /// </summary>
    public class RegistryListenerService : IDisposable
    {
        private readonly Log _log;
        private PinionCore.Remote.Soul.IListenable? _listener;
        private bool _disposed = false;

        System.Action _Dispose;

        public RegistryListenerService(Log log)
        {
            _Dispose = () => { };
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        /// <summary>
        /// 啟動 Registry TCP 監聽器並綁定到 Router Registry 端點
        /// </summary>
        /// <param name="registryEndpoint">Router 的 Registry 端點</param>
        /// <param name="port">TCP 端口</param>
        public void Start(IService registryEndpoint, int port)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RegistryListenerService));

            if (registryEndpoint == null)
                throw new ArgumentNullException(nameof(registryEndpoint));

            try
            {
                // 建立 TCP 監聽器
                var tcpListener  = new Tcp.Listener();
                tcpListener.Bind(port, backlog: 100);
                _listener = tcpListener;                
                _log.WriteInfo($"Registry TCP 監聽已啟動，端口: {port}");

                // 事件驅動整合：StreamableEnterEvent → Registry.Join
                _listener.StreamableEnterEvent += registryEndpoint.Join;
                _listener.StreamableLeaveEvent += registryEndpoint.Leave;

                _log.WriteInfo("Registry 監聽器已成功綁定到 Registry 端點");

                _Dispose = () =>
                {
                    _listener.StreamableEnterEvent -= registryEndpoint.Join;
                    _listener.StreamableLeaveEvent -= registryEndpoint.Leave;
                    tcpListener.Close();
                };
            }
            catch (Exception ex)
            {
                _log.WriteInfo(() => $"Registry 監聽器啟動失敗: {ex.Message}");
                Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _log.WriteInfo("關閉 Registry 監聽器...");

            try
            {
                _Dispose();
            }
            catch (Exception ex)
            {
                _log.WriteInfo(() => $"關閉 Registry 監聽器時發生錯誤: {ex.Message}");
            }

            _disposed = true;
        }
    }
}
