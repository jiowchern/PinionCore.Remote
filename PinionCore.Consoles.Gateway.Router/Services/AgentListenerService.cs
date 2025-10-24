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
                webListener.Bind($"http://0.0.0.0:{webPort}/");
                _webListener = webListener;
                _log.WriteInfo($"Agent WebSocket 監聽已啟動，端口: {webPort}");

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
