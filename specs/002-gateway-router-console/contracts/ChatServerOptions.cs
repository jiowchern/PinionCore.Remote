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
Enhanced Chat Server 使用說明:

  PinionCore.Consoles.Chat1.Server [選項]

直連模式選項:
  --tcp-port=PORT             直連 TCP 監聽端口 (可選)
  --web-port=PORT             直連 WebSocket 監聽端口 (可選)

Gateway 模式選項:
  --router-host=HOST          Router 主機位址 (可選)
  --router-port=PORT          Router Registry 端口 (可選)
  --group=ID                  服務群組 ID (預設: 1)

範例:
  # 只啟用直連模式
  PinionCore.Consoles.Chat1.Server --tcp-port=23916 --web-port=23917

  # 只啟用 Gateway 模式
  PinionCore.Consoles.Chat1.Server --router-host=127.0.0.1 --router-port=8003

  # 最大相容性模式 (同時啟用三種連線來源)
  PinionCore.Consoles.Chat1.Server --tcp-port=23916 --web-port=23917 --router-host=127.0.0.1 --router-port=8003 --group=1
";
        }
    }
}
