using System;
using System.Linq.Expressions;

namespace PinionCore.Consoles.Gateway.Router.Infrastructure
{
    /// <summary>
    /// 日誌配置，封裝 PinionCore.Utility.Log 與 LogFileRecorder
    /// </summary>
    public class LoggingConfiguration : IDisposable
    {
        private readonly PinionCore.Utility.Log _log;
        private readonly PinionCore.Utility.LogFileRecorder _fileRecorder;

        public PinionCore.Utility.Log Log => _log;

        public LoggingConfiguration(string fileNamePrefix)
        {
            _log = PinionCore.Utility.Log.Instance;
            _fileRecorder = new PinionCore.Utility.LogFileRecorder(fileNamePrefix);

            // 配置 stdout 輸出
            _log.RecordEvent += Console.WriteLine;

            // 配置檔案輸出
            _log.RecordEvent += _fileRecorder.Record;
        }

        /// <summary>
        /// 寫入資訊級別日誌
        /// </summary>
        public void WriteInfo(string message)
        {
            _log.WriteInfo(message);
        }

        /// <summary>
        /// 寫入資訊級別日誌 (延遲求值)
        /// </summary>
        public void WriteInfo(Expression<Func<string>> messageFunc)
        {
            _log.WriteInfo(messageFunc);
        }

        /// <summary>
        /// 寫入警告級別日誌 (使用 WriteInfo 加上前綴)
        /// </summary>
        public void WriteWarning(string message)
        {
            _log.WriteInfo($"[WARNING] {message}");
        }

        /// <summary>
        /// 寫入錯誤級別日誌 (使用 WriteInfo 加上前綴)
        /// </summary>
        public void WriteError(string message)
        {
            _log.WriteInfo($"[ERROR] {message}");
        }

        /// <summary>
        /// 寫入除錯級別日誌
        /// </summary>
        public void WriteDebug(string message)
        {
            _log.WriteDebug(message);
        }

        /// <summary>
        /// 關閉日誌系統
        /// </summary>
        public void Shutdown()
        {
            _fileRecorder.Save();
            _fileRecorder.Close();
            _log.Shutdown();  // 等待非同步佇列清空
        }

        public void Dispose()
        {
            Shutdown();
        }
    }
}
