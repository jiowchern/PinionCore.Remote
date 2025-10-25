using System;

namespace PinionCore.Consoles.Chat1.Client.Configuration
{
    /// <summary>
    /// Enhanced Chat Client 命令列參數配置 (支援 Router 模式與直連模式)
    /// </summary>
    public class ChatClientOptions
    {
        // ===== Router 模式參數 =====

        /// <summary>
        /// Router 主機位址 (可選,預設不啟用)
        /// </summary>
        public string RouterHost { get; set; }

        /// <summary>
        /// Router Agent 端口 (可選,預設不啟用)
        /// </summary>
        public int? RouterPort { get; set; }

        /// <summary>
        /// 使用 WebSocket 協議連接 (預設 false，使用 TCP)
        /// </summary>
        public bool UseWebSocket { get; set; }

        // ===== 模式判斷屬性 =====

        /// <summary>
        /// 是否啟用 Router 模式
        /// </summary>
        public bool HasRouterMode => !string.IsNullOrEmpty(RouterHost) && RouterPort.HasValue;

        /// <summary>
        /// 驗證配置有效性
        /// </summary>
        /// <param name="error">錯誤訊息輸出</param>
        /// <returns>是否有效</returns>
        public bool Validate(out string error)
        {
            if (HasRouterMode)
            {
                if (string.IsNullOrWhiteSpace(RouterHost))
                {
                    error = "Router 主機位址不可為空";
                    return false;
                }

                if (!RouterPort.HasValue)
                {
                    error = "必須同時提供 --router-host 與 --router-port";
                    return false;
                }

                if (!IsValidPort(RouterPort.Value))
                {
                    error = $"Router 端口無效: {RouterPort.Value} (有效範圍: 1-65535)";
                    return false;
                }
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
Enhanced Chat Client 使用說明
=============================

用途:
  聊天客戶端應用程式,支援兩種連線模式:
  1. Router 模式 - 透過 Gateway Router 連接 (推薦,支援負載平衡與服務發現)
  2. 直連模式 - 直接連接到 Chat Server (開發測試用)

命令列語法:
  PinionCore.Consoles.Chat1.Client [選項]

Router 模式選項:
  --router-host=HOST          Router 主機位址 (可選,支援主機名或 IP)
  --router-port=PORT          Router Agent 端口 (可選)
                              - TCP 模式使用 8001
                              - WebSocket 模式使用 8002
  --websocket                 使用 WebSocket 協議 (預設使用 TCP)

通用選項:
  --help, -h                  顯示此使用說明

模式決策:
  - 生產環境: 使用 Router 模式 (自動路由到可用服務)
  - 開發測試: 使用直連模式 (直接連接特定服務)
  - Web 瀏覽器: 必須使用 Router 模式 + WebSocket 協議

範例:
  # 使用 Router 模式連接 (TCP)
  PinionCore.Consoles.Chat1.Client --router-host=127.0.0.1 --router-port=8001

  # 使用 Router 模式連接 (WebSocket)
  PinionCore.Consoles.Chat1.Client --router-host=127.0.0.1 --router-port=8002 --websocket

  # 連接到 Docker 容器中的 Router
  PinionCore.Consoles.Chat1.Client --router-host=127.0.0.1 --router-port=8001

  # 使用傳統直連模式 (啟動後互動輸入 Server IP 與端口)
  PinionCore.Consoles.Chat1.Client

  # 顯示使用說明
  PinionCore.Consoles.Chat1.Client --help

Docker 外部連接範例:
  # 本機客戶端連接到 Docker 容器中的 Router (TCP)
  PinionCore.Consoles.Chat1.Client --router-host=127.0.0.1 --router-port=8001

  # 本機客戶端連接到 Docker 容器中的 Router (WebSocket)
  PinionCore.Consoles.Chat1.Client --router-host=127.0.0.1 --router-port=8002 --websocket

  # 連接到遠端 Docker 主機
  PinionCore.Consoles.Chat1.Client --router-host=192.168.1.100 --router-port=8001

協議選擇:
  - TCP: 性能較佳,適合桌面應用程式 (預設)
  - WebSocket: 支援瀏覽器,穿透防火牆能力較強 (使用 --websocket)

連線失敗排查:
  1. 檢查 Router 是否運行:
     docker ps | grep gateway-router
     netstat -an | findstr ""8001 8002""

  2. 檢查網路連接:
     ping <router-host>
     telnet <router-host> <router-port>

  3. 檢查防火牆規則:
     確認端口 8001/8002 未被防火牆阻擋

  4. 查看 Router 日誌:
     docker logs gateway-router

常見錯誤:
  - ""連線失敗"": Router 未啟動或端口錯誤
  - ""等待路由分配"": 無可用的 Chat Server (檢查 Registry 連線)
  - ""協議不符"": TCP/WebSocket 端口使用錯誤

更多資訊:
  - 架構文件: specs/002-gateway-router-console/spec.md
  - Docker 文件: docker/DOCKER.md
  - 專案首頁: https://github.com/jiowchern/PinionCore.Remote
";
        }
    }
}
