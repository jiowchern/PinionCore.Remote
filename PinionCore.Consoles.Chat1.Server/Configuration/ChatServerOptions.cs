using System;

namespace PinionCore.Consoles.Chat1.Server.Configuration
{
    /// <summary>
    /// Enhanced Chat Server 命令列參數配置 (支援最大相容性模式)
    /// </summary>
    public class ChatServerOptions
    {
        // ===== 直連模式參數 =====

        /// <summary>
        /// 直連 TCP 監聽端口 (可選,預設不啟用)
        /// </summary>
        public int? TcpPort { get; set; }

        /// <summary>
        /// 直連 WebSocket 監聽端口 (可選,預設不啟用)
        /// </summary>
        public int? WebPort { get; set; }

        // ===== Gateway 模式參數 =====

        /// <summary>
        /// Router 主機位址 (可選,預設不啟用)
        /// </summary>
        public string RouterHost { get; set; }

        /// <summary>
        /// Router Registry 端口 (可選,預設不啟用)
        /// </summary>
        public int? RouterPort { get; set; }

        /// <summary>
        /// 服務群組 ID (預設 1)
        /// </summary>
        public uint Group { get; set; } = 1;

        // ===== 模式判斷屬性 =====

        /// <summary>
        /// 是否啟用直連模式 (TCP 或 WebSocket)
        /// </summary>
        public bool HasDirectMode => TcpPort.HasValue || WebPort.HasValue;

        /// <summary>
        /// 是否啟用 Gateway 模式
        /// </summary>
        public bool HasGatewayMode => !string.IsNullOrEmpty(RouterHost) && RouterPort.HasValue;

        /// <summary>
        /// 是否啟用任何模式
        /// </summary>
        public bool HasAnyMode => HasDirectMode || HasGatewayMode;

        /// <summary>
        /// 是否為最大相容性模式 (同時啟用直連與 Gateway)
        /// </summary>
        public bool IsMaxCompatibilityMode => HasDirectMode && HasGatewayMode;

        /// <summary>
        /// 驗證配置有效性
        /// </summary>
        /// <param name="error">錯誤訊息輸出</param>
        /// <returns>是否有效</returns>
        public bool Validate(out string error)
        {
            if (!HasAnyMode)
            {
                error = "必須至少提供 --tcp-port、--web-port 或 --router-host 其中一個參數";
                return false;
            }

            if (TcpPort.HasValue && !IsValidPort(TcpPort.Value))
            {
                error = $"TCP 端口無效: {TcpPort.Value} (有效範圍: 1-65535)";
                return false;
            }

            if (WebPort.HasValue && !IsValidPort(WebPort.Value))
            {
                error = $"WebSocket 端口無效: {WebPort.Value} (有效範圍: 1-65535)";
                return false;
            }

            if (TcpPort.HasValue && WebPort.HasValue && TcpPort.Value == WebPort.Value)
            {
                error = "端口配置衝突:TCP 與 WebSocket 必須使用不同端口";
                return false;
            }

            if (HasGatewayMode)
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
Enhanced Chat Server 使用說明
==============================

用途:
  提供聊天服務,支援三種連線模式:
  1. 直連 TCP 模式 - 傳統客戶端直接連接
  2. 直連 WebSocket 模式 - Web 客戶端直接連接
  3. Gateway 模式 - 透過 Router 路由連接 (推薦生產環境)

命令列語法:
  PinionCore.Consoles.Chat1.Server [選項]

直連模式選項:
  --tcp-port=PORT             直連 TCP 監聽端口 (可選)
  --web-port=PORT             直連 WebSocket 監聽端口 (可選)

Gateway 模式選項:
  --router-host=HOST          Router 主機位址 (可選,支援主機名或 IP)
  --router-port=PORT          Router Registry 端口 (可選)
  --group=ID                  服務群組 ID (預設: 1)

通用選項:
  --help, -h                  顯示此使用說明

連線模式決策:
  - 開發測試: 使用直連模式 (--tcp-port 或 --web-port)
  - 生產部署: 使用 Gateway 模式 (--router-host + --router-port)
  - 平滑遷移: 使用最大相容性模式 (同時啟用所有選項)

範例:
  # 只啟用直連模式
  PinionCore.Consoles.Chat1.Server --tcp-port=23916 --web-port=23917

  # 只啟用 Gateway 模式 (推薦)
  PinionCore.Consoles.Chat1.Server --router-host=127.0.0.1 --router-port=8003 --group=1

  # 最大相容性模式 (支援所有連線方式)
  PinionCore.Consoles.Chat1.Server --tcp-port=23916 --web-port=23917 --router-host=127.0.0.1 --router-port=8003 --group=1

  # 顯示使用說明
  PinionCore.Consoles.Chat1.Server --help

Docker 使用範例:
  # 構建 Docker 映像檔
  docker build -f docker/Dockerfile.chatserver -t chat-server:latest .

  # 純 Gateway 模式 (推薦,無需暴露端口)
  docker run -d --name chat-server-1 \
    --network gateway-network \
    chat-server:latest \
    --router-host=router --router-port=8003 --group=1

  # 最大相容性模式 (同時支援直連與 Gateway)
  docker run -d --name chat-server-1 \
    -p 23916:23916 -p 23917:23917 \
    --network gateway-network \
    chat-server:latest \
    --tcp-port=23916 --web-port=23917 --router-host=router --router-port=8003 --group=1

  # 查看使用說明
  docker run --rm chat-server:latest --help

  # 使用 Docker Compose (推薦)
  cd docker && docker-compose up -d

環境變數 (Docker 建議):
  TCP_PORT                    直連 TCP 端口 (對應 --tcp-port)
  WEB_PORT                    直連 WebSocket 端口 (對應 --web-port)
  ROUTER_HOST                 Router 主機位址 (對應 --router-host)
  ROUTER_PORT                 Router Registry 端口 (對應 --router-port)
  GROUP                       服務群組 ID (對應 --group)

  注意: 命令列參數優先於環境變數。

日誌:
  - 控制台輸出: stdout (即時日誌)
  - 檔案輸出: ./ChatServer_yyyy_MM_dd_HH_mm_ss.log
  - 日誌等級: Info (包含連線狀態、Registry 註冊、玩家活動)

連線狀態檢查:
  # 查看 Registry 連線狀態
  docker logs chat-server-1 | grep ""Registry 狀態""

  # 查看當前連線數
  docker exec chat-server-1 netstat -an | grep ESTABLISHED

斷線重連:
  Gateway 模式支援自動重連 (指數退避,1-60 秒間隔)。
  斷線後會自動嘗試重新連接到 Router。

更多資訊:
  - 架構文件: specs/002-gateway-router-console/spec.md
  - Docker 文件: docker/DOCKER.md
  - 專案首頁: https://github.com/jiowchern/PinionCore.Remote
";
        }
    }
}
