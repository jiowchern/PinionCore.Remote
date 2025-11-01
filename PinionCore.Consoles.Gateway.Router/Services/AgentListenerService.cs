using PinionCore.Network;
using PinionCore.Remote.Server;
using PinionCore.Remote.Soul;
using PinionCore.Utility;
using PinionCore.Consoles.Gateway.Router.Workers;
using Tcp = PinionCore.Remote.Server.Tcp;
using Web = PinionCore.Remote.Server.Web;

namespace PinionCore.Consoles.Gateway.Router.Services
{
    /// <summary>
    /// 管理 Agent 監聽器 (TCP + WebSocket)
    /// 使用事件驅動模式整合到 Router.Session 端點
    /// </summary>
    public class AgentListenerService : IDisposable
    {
        private readonly Log _log;
        private readonly AgentWorkerPool _workerPool;
        private PinionCore.Remote.Soul.IListenable? _tcpListener;
        private PinionCore.Remote.Soul.IListenable? _webListener;
        private PinionCore.Remote.Soul.IListenable? _aggregator;
        private bool _disposed = false;

        // T082: Agent 連接統計
        private int _tcpAgentCount = 0;
        private int _webAgentCount = 0;

        System.Action _Dispose;

        public AgentListenerService(Log log, AgentWorkerPool workerPool)
        {
            _Dispose = () => { };
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _workerPool = workerPool ?? throw new ArgumentNullException(nameof(workerPool));
        }

        /// <summary>
        /// 啟動 Agent TCP + WebSocket 監聽器並綁定到 Router Session 端點
        /// </summary>
        /// <param name="sessionEndpoint">Router 的 Session 端點</param>
        /// <param name="tcpPort">TCP 端口</param>
        /// <param name="webPort">WebSocket 端口</param>
        public void Start(IService sessionEndpoint, int tcpPort, int webPort)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AgentListenerService));

            if (sessionEndpoint == null)
                throw new ArgumentNullException(nameof(sessionEndpoint));

            try
            {
                // 建立 TCP 監聽器
                var tcpListener = new Tcp.Listener();
                tcpListener.Bind(tcpPort, backlog: 100);
                _tcpListener = tcpListener;
                _log.WriteInfo($"Agent TCP 監聽已啟動，端口: {tcpPort}");

                // 建立 WebSocket 監聽器
                var webListener = new Web.Listener();
                webListener.Bind($"http://+:{webPort}/");  // 修復: 綁定到所有 IP 地址而非僅 localhost
                _webListener = webListener;
                _log.WriteInfo($"Agent WebSocket 監聽已啟動，端口: {webPort}");

                // T082: 分別訂閱 TCP 和 WebSocket 事件以追蹤協議類型
                _tcpListener.StreamableEnterEvent += _OnTcpAgentConnected;
                _tcpListener.StreamableLeaveEvent += _OnTcpAgentDisconnected;
                _webListener.StreamableEnterEvent += _OnWebAgentConnected;
                _webListener.StreamableLeaveEvent += _OnWebAgentDisconnected;

                // 使用 PinionCore.Remote.Soul.ListenableAggregator 合併兩個監聽器
                var aggregator = new PinionCore.Remote.Soul.ListenableAggregator();
                aggregator.Add(_tcpListener);
                aggregator.Add(_webListener);
                _aggregator = aggregator;

                // 事件驅動整合：StreamableEnterEvent → Session.Join
                _aggregator.StreamableEnterEvent += sessionEndpoint.Join;
                _aggregator.StreamableLeaveEvent += sessionEndpoint.Leave;

                _log.WriteInfo("Agent 監聽器已成功綁定到 Session 端點");

                _Dispose = () =>
                {
                    _tcpListener.StreamableEnterEvent -= _OnTcpAgentConnected;
                    _tcpListener.StreamableLeaveEvent -= _OnTcpAgentDisconnected;
                    _webListener.StreamableEnterEvent -= _OnWebAgentConnected;
                    _webListener.StreamableLeaveEvent -= _OnWebAgentDisconnected;
                    _aggregator.StreamableEnterEvent -= sessionEndpoint.Join;
                    _aggregator.StreamableLeaveEvent -= sessionEndpoint.Leave;
                    tcpListener.Close();
                    webListener.Close();
                };
            }
            catch (Exception ex)
            {
                _log.WriteInfo(() => $"Agent 監聽器啟動失敗: {ex.Message}");
                Dispose();
                throw;
            }
        }

        /// <summary>
        /// T082: 處理 TCP Agent 連接事件
        /// </summary>
        private void _OnTcpAgentConnected(IStreamable streamable)
        {
            _tcpAgentCount++;
            _log.WriteInfo(() => $"[TCP] Agent 連接建立 (TCP: {_tcpAgentCount}, WebSocket: {_webAgentCount}, 總計: {_tcpAgentCount + _webAgentCount})");
        }

        /// <summary>
        /// T082: 處理 TCP Agent 斷線事件
        /// </summary>
        private void _OnTcpAgentDisconnected(IStreamable streamable)
        {
            _tcpAgentCount--;
            _log.WriteInfo(() => $"[TCP] Agent 連接中斷 (TCP: {_tcpAgentCount}, WebSocket: {_webAgentCount}, 總計: {_tcpAgentCount + _webAgentCount})");
        }

        /// <summary>
        /// T082: 處理 WebSocket Agent 連接事件
        /// </summary>
        private void _OnWebAgentConnected(IStreamable streamable)
        {
            _webAgentCount++;
            _log.WriteInfo(() => $"[WebSocket] Agent 連接建立 (TCP: {_tcpAgentCount}, WebSocket: {_webAgentCount}, 總計: {_tcpAgentCount + _webAgentCount})");
        }

        /// <summary>
        /// T082: 處理 WebSocket Agent 斷線事件
        /// </summary>
        private void _OnWebAgentDisconnected(IStreamable streamable)
        {
            _webAgentCount--;
            _log.WriteInfo(() => $"[WebSocket] Agent 連接中斷 (TCP: {_tcpAgentCount}, WebSocket: {_webAgentCount}, 總計: {_tcpAgentCount + _webAgentCount})");
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            
            _log.WriteInfo("關閉 Agent 監聽器...");

            try
            {                
                _Dispose();
            }
            catch (Exception ex)
            {
                _log.WriteInfo(() => $"關閉 Agent 監聽器時發生錯誤: {ex.Message}");
            }

            _disposed = true;
        }
    }
}
