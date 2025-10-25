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
        private int _registryCount = 0;
        private IService? _registryEndpoint;

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

            _registryEndpoint = registryEndpoint;

            try
            {
                // 建立 TCP 監聽器
                var tcpListener  = new Tcp.Listener();
                tcpListener.Bind(port, backlog: 100);
                _listener = tcpListener;
                _log.WriteInfo($"Registry TCP 監聽已啟動，端口: {port}");

                // T033 & T034: 事件驅動整合，添加連接/斷線日誌
                _listener.StreamableEnterEvent += _OnRegistryConnected;
                _listener.StreamableLeaveEvent += _OnRegistryDisconnected;

                _log.WriteInfo("Registry 監聽器已成功綁定到 Registry 端點");

                _Dispose = () =>
                {
                    _listener.StreamableEnterEvent -= _OnRegistryConnected;
                    _listener.StreamableLeaveEvent -= _OnRegistryDisconnected;
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

        /// <summary>
        /// T033: 處理 Registry 連接事件
        /// </summary>
        private void _OnRegistryConnected(IStreamable streamable)
        {
            _registryCount++;
            // T082: 添加連接時間戳
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _log.WriteInfo(() => $"Registry 連接建立 [{timestamp}] (當前連接數: {_registryCount})");

            // 將連接傳遞給 Registry 端點處理
            _registryEndpoint?.Join(streamable);
        }

        /// <summary>
        /// T034: 處理 Registry 斷線事件
        /// </summary>
        private void _OnRegistryDisconnected(IStreamable streamable)
        {
            _registryCount--;
            // T082: 添加斷線時間戳
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _log.WriteInfo(() => $"Registry 連接中斷 [{timestamp}] (當前連接數: {_registryCount})");

            // 通知 Registry 端點處理斷線
            _registryEndpoint?.Leave(streamable);
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
