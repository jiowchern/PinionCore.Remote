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
Enhanced Chat Client 使用說明:

  PinionCore.Consoles.Chat1.Client [選項]

Router 模式選項:
  --router-host=HOST          Router 主機位址 (可選)
  --router-port=PORT          Router Agent 端口 (可選)

範例:
  # 使用 Router 模式連接
  PinionCore.Consoles.Chat1.Client --router-host=127.0.0.1 --router-port=8001

  # 使用傳統直連模式 (不提供參數,啟動後互動輸入 Server IP 與端口)
  PinionCore.Consoles.Chat1.Client
";
        }
    }
}
