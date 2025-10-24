using System;

namespace PinionCore.Consoles.Chat1.Server.Services
{
    /// <summary>
    /// 指數退避重連器,實作重連延遲計算:1秒、2秒、4秒...最大60秒
    /// </summary>
    public class ExponentialBackoffReconnector
    {
        private int _retryCount = 0;
        private const int InitialDelayMs = 1000;   // 初始延遲:1 秒
        private const int MaxDelayMs = 60000;      // 最大延遲:60 秒

        /// <summary>
        /// 計算當前重連延遲時間（毫秒）
        /// </summary>
        public int CalculateDelay()
        {
            return Math.Min((int)Math.Pow(2, _retryCount) * InitialDelayMs, MaxDelayMs);
        }

        /// <summary>
        /// 增加重試計數
        /// </summary>
        public void IncrementRetryCount()
        {
            _retryCount++;
        }

        /// <summary>
        /// 重置重試計數（連接成功時調用）
        /// </summary>
        public void ResetRetryCount()
        {
            _retryCount = 0;
        }

        /// <summary>
        /// 獲取當前重試次數
        /// </summary>
        public int RetryCount => _retryCount;
    }
}
