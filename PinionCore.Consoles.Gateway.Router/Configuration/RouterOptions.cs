using System;

namespace PinionCore.Consoles.Gateway.Router.Configuration
{
    /// <summary>
    /// Router Console 命令列參數配置
    /// </summary>
    public class RouterOptions
    {
        /// <summary>
        /// Agent TCP 監聽端口 (預設 8001)
        /// </summary>
        public int AgentTcpPort { get; set; } = 8001;

        /// <summary>
        /// Agent WebSocket 監聽端口 (預設 8002)
        /// </summary>
        public int AgentWebPort { get; set; } = 8002;

        /// <summary>
        /// Registry TCP 監聽端口 (預設 8003)
        /// </summary>
        public int RegistryTcpPort { get; set; } = 8003;

        /// <summary>
        /// 驗證配置有效性
        /// </summary>
        /// <param name="error">錯誤訊息輸出</param>
        /// <returns>是否有效</returns>
        public bool Validate(out string error)
        {
            if (!IsValidPort(AgentTcpPort))
            {
                error = $"Agent TCP 端口無效: {AgentTcpPort} (有效範圍: 1-65535)";
                return false;
            }

            if (!IsValidPort(AgentWebPort))
            {
                error = $"Agent WebSocket 端口無效: {AgentWebPort} (有效範圍: 1-65535)";
                return false;
            }

            if (!IsValidPort(RegistryTcpPort))
            {
                error = $"Registry TCP 端口無效: {RegistryTcpPort} (有效範圍: 1-65535)";
                return false;
            }

            if (AgentTcpPort == AgentWebPort || AgentTcpPort == RegistryTcpPort || AgentWebPort == RegistryTcpPort)
            {
                error = "端口配置衝突:Agent TCP、Agent WebSocket、Registry TCP 必須使用不同端口";
                return false;
            }

            error = null!;
            return true;
        }

        /// <summary>
        /// 檢查端口號是否有效
        /// </summary>
        private bool IsValidPort(int port) => port >= 1 && port <= 65535;

        /// <summary>
        /// 產生使用說明字串
        /// </summary>
        public static string GetUsageString()
        {
            return @"
Gateway Router Console 使用說明
=================================

用途:
  提供 Gateway Router 服務，負責將 Agent (客戶端) 連線路由到可用的 Registry (遊戲服務)。
  支援 TCP 與 WebSocket 協議，實現負載平衡與自動服務發現。

命令列語法:
  PinionCore.Consoles.Gateway.Router [選項]

選項:
  --agent-tcp-port=PORT       Agent TCP 監聽端口 (預設: 8001)
  --agent-web-port=PORT       Agent WebSocket 監聽端口 (預設: 8002)
  --registry-tcp-port=PORT    Registry TCP 監聽端口 (預設: 8003)
  --help, -h                  顯示此使用說明

端口範圍:
  所有端口必須在 1-65535 範圍內，且不可重複。

範例:
  # 使用預設端口啟動
  PinionCore.Consoles.Gateway.Router

  # 自訂端口啟動
  PinionCore.Consoles.Gateway.Router --agent-tcp-port=9001 --agent-web-port=9002 --registry-tcp-port=9003

  # 顯示使用說明
  PinionCore.Consoles.Gateway.Router --help

Docker 使用範例:
  # 構建 Docker 映像檔
  docker build -f docker/Dockerfile.router -t gateway-router:latest .

  # 使用預設端口運行
  docker run -d --name router -p 8001:8001 -p 8002:8002 -p 8003:8003 gateway-router:latest

  # 使用自訂端口運行
  docker run -d --name router -p 9001:9001 -p 9002:9002 -p 9003:9003 \
    gateway-router:latest \
    --agent-tcp-port=9001 --agent-web-port=9002 --registry-tcp-port=9003

  # 查看使用說明
  docker run --rm gateway-router:latest --help

  # 使用 Docker Compose
  cd docker && docker-compose up -d

環境變數 (Docker 建議):
  AGENT_TCP_PORT              Agent TCP 端口 (對應 --agent-tcp-port)
  AGENT_WEB_PORT              Agent WebSocket 端口 (對應 --agent-web-port)
  REGISTRY_TCP_PORT           Registry TCP 端口 (對應 --registry-tcp-port)

  注意: 命令列參數優先於環境變數。

日誌:
  - 控制台輸出: stdout (即時日誌)
  - 檔案輸出: ./RouterConsole_yyyy_MM_dd_HH_mm_ss.log
  - 日誌等級: Info (包含連線事件、路由分配、錯誤訊息)

連線檢查:
  # Windows
  netstat -an | findstr ""8001 8002 8003""

  # Linux/macOS
  netstat -an | grep -E ""8001|8002|8003""

  # Docker 容器內
  docker exec router netstat -an | grep 8001

更多資訊:
  - 架構文件: specs/002-gateway-router-console/spec.md
  - Docker 文件: docker/DOCKER.md
  - 專案首頁: https://github.com/jiowchern/PinionCore.Remote
";
        }
    }
}
