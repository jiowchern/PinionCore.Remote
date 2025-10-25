using System;
using PinionCore.Utility;

namespace PinionCore.Consoles.Gateway.Router.Infrastructure
{
    /// <summary>
    /// T083: 標準化錯誤處理輔助類別
    /// 提供統一的錯誤日誌格式與錯誤處理模式
    /// </summary>
    public static class ErrorHandler
    {
        /// <summary>
        /// 記錄詳細錯誤資訊到日誌（包含堆疊追蹤）
        /// </summary>
        /// <param name="log">日誌實例</param>
        /// <param name="context">錯誤發生的上下文描述</param>
        /// <param name="ex">例外物件</param>
        public static void LogError(Log log, string context, Exception ex)
        {
            if (log == null || ex == null)
                return;

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            log.WriteInfo(() => $"[錯誤] {context}");
            log.WriteInfo(() => $"  時間: {timestamp}");
            log.WriteInfo(() => $"  類型: {ex.GetType().Name}");
            log.WriteInfo(() => $"  訊息: {ex.Message}");

            // 記錄內部例外
            if (ex.InnerException != null)
            {
                log.WriteInfo(() => $"  內部錯誤: {ex.InnerException.Message}");
            }

            // 記錄堆疊追蹤（僅前 3 個堆疊幀，避免日誌過長）
            if (!string.IsNullOrWhiteSpace(ex.StackTrace))
            {
                var stackLines = ex.StackTrace.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                var frameCount = Math.Min(stackLines.Length, 3);
                log.WriteInfo("  堆疊追蹤:");
                for (int i = 0; i < frameCount; i++)
                {
                    log.WriteInfo(() => $"    {stackLines[i].Trim()}");
                }
            }
        }

        /// <summary>
        /// 記錄簡化錯誤訊息（僅包含錯誤訊息，不含堆疊追蹤）
        /// </summary>
        /// <param name="log">日誌實例</param>
        /// <param name="context">錯誤發生的上下文描述</param>
        /// <param name="ex">例外物件</param>
        public static void LogSimpleError(Log log, string context, Exception ex)
        {
            if (log == null || ex == null)
                return;

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            log.WriteInfo(() => $"[錯誤] {context}: {ex.Message} [{timestamp}]");
        }

        /// <summary>
        /// 記錄警告訊息
        /// </summary>
        /// <param name="log">日誌實例</param>
        /// <param name="context">警告發生的上下文描述</param>
        /// <param name="ex">例外物件（可選）</param>
        public static void LogWarning(Log log, string context, Exception ex = null)
        {
            if (log == null)
                return;

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            if (ex != null)
            {
                log.WriteInfo(() => $"[警告] {context}: {ex.Message} [{timestamp}]");
            }
            else
            {
                log.WriteInfo(() => $"[警告] {context} [{timestamp}]");
            }
        }

        /// <summary>
        /// 安全執行 Dispose 操作（捕獲並記錄錯誤）
        /// </summary>
        /// <param name="log">日誌實例</param>
        /// <param name="disposable">要釋放的物件</param>
        /// <param name="objectName">物件名稱（用於日誌）</param>
        public static void SafeDispose(Log log, IDisposable disposable, string objectName)
        {
            if (disposable == null)
                return;

            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                LogWarning(log, $"釋放 {objectName} 時發生錯誤", ex);
            }
        }

        /// <summary>
        /// 執行操作並捕獲所有例外（記錄錯誤但不重新拋出）
        /// </summary>
        /// <param name="log">日誌實例</param>
        /// <param name="action">要執行的操作</param>
        /// <param name="context">操作上下文描述</param>
        /// <returns>是否成功執行（無例外）</returns>
        public static bool TryExecute(Log log, Action action, string context)
        {
            if (action == null)
                return false;

            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                LogError(log, context, ex);
                return false;
            }
        }
    }
}
