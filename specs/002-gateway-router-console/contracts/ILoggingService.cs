using System;

namespace PinionCore.Consoles.Common.Logging
{
    /// <summary>
    /// 日誌服務介面,封裝 PinionCore.Utility.Log 與 LogFileRecorder
    /// </summary>
    public interface ILoggingService : IDisposable
    {
        /// <summary>
        /// 寫入資訊級別日誌
        /// </summary>
        /// <param name="message">訊息內容</param>
        void WriteInfo(string message);

        /// <summary>
        /// 寫入資訊級別日誌 (延遲求值)
        /// </summary>
        /// <param name="messageFunc">訊息產生函數</param>
        void WriteInfo(Func<string> messageFunc);

        /// <summary>
        /// 寫入警告級別日誌
        /// </summary>
        /// <param name="message">訊息內容</param>
        void WriteWarning(string message);

        /// <summary>
        /// 寫入錯誤級別日誌
        /// </summary>
        /// <param name="message">訊息內容</param>
        void WriteError(string message);

        /// <summary>
        /// 寫入除錯級別日誌 (包含堆疊追蹤)
        /// </summary>
        /// <param name="message">訊息內容</param>
        void WriteDebug(string message);

        /// <summary>
        /// 儲存並關閉日誌檔案
        /// </summary>
        void Shutdown();
    }
}
