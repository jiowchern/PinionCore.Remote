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

            error = null;
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
Gateway Router Console 使用說明:

  PinionCore.Consoles.Gateway.Router [選項]

選項:
  --agent-tcp-port=PORT       Agent TCP 監聽端口 (預設: 8001)
  --agent-web-port=PORT       Agent WebSocket 監聽端口 (預設: 8002)
  --registry-tcp-port=PORT    Registry TCP 監聽端口 (預設: 8003)

範例:
  # 使用預設端口啟動
  PinionCore.Consoles.Gateway.Router

  # 自訂端口啟動
  PinionCore.Consoles.Gateway.Router --agent-tcp-port=9001 --agent-web-port=9002 --registry-tcp-port=9003
";
        }
    }
}
