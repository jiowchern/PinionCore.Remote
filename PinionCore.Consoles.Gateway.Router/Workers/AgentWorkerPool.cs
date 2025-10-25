using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PinionCore.Utility;
using PinionCore.Consoles.Gateway.Router.Infrastructure;

namespace PinionCore.Consoles.Gateway.Router.Workers
{
    /// <summary>
    /// AgentWorkerPool 集中管理所有 AgentWorker，支援批次關閉
    /// </summary>
    public class AgentWorkerPool : IDisposable
    {
        private readonly List<AgentWorker> _workers = new List<AgentWorker>();
        private readonly object _lock = new object();
        private readonly Log _log;

        public AgentWorkerPool(Log log = null)
        {
            _log = log ?? Log.Instance;
        }

        /// <summary>
        /// 當前 Worker 數量
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _workers.Count;
                }
            }
        }

        /// <summary>
        /// 添加 Worker 到 Pool
        /// </summary>
        public void Add(AgentWorker worker)
        {
            if (worker == null)
                throw new ArgumentNullException(nameof(worker));

            lock (_lock)
            {
                _workers.Add(worker);
            }

            // 訂閱錯誤事件，自動從 pool 移除
            worker.ErrorEvent += (ex) =>
            {
                Remove(worker);
            };
        }

        /// <summary>
        /// 從 Pool 移除 Worker
        /// </summary>
        public void Remove(AgentWorker worker)
        {
            if (worker == null)
                return;

            lock (_lock)
            {
                _workers.Remove(worker);
            }

            // 清理 worker
            try
            {
                worker.Dispose();
            }
            catch (Exception ex)
            {
                // T083: 記錄 Dispose 錯誤
                ErrorHandler.LogWarning(_log, $"移除 Agent Worker [{worker?.Id}] 時發生錯誤", ex);
            }
        }

        /// <summary>
        /// 批次關閉所有 Worker
        /// </summary>
        public async Task DisposeAllAsync(CancellationToken cancellationToken)
        {
            List<AgentWorker> workersCopy;
            lock (_lock)
            {
                workersCopy = new List<AgentWorker>(_workers);
                _workers.Clear();
            }

            if (workersCopy.Count == 0)
                return;

            _log.WriteInfo(() => $"開始關閉 {workersCopy.Count} 個 Agent Worker...");

            // 平行關閉所有 Worker
            var disposeTasks = workersCopy.Select(w => Task.Run(() =>
            {
                try
                {
                    w.Dispose();
                }
                catch (Exception ex)
                {
                    // T083: 記錄個別 worker 的 Dispose 錯誤
                    ErrorHandler.LogWarning(_log, $"關閉 Agent Worker [{w?.Id}] 時發生錯誤", ex);
                }
            }, cancellationToken));

            try
            {
                await Task.WhenAll(disposeTasks);
                _log.WriteInfo(() => $"成功關閉 {workersCopy.Count} 個 Agent Worker");
            }
            catch (Exception ex)
            {
                // T083: 記錄整體關閉錯誤
                ErrorHandler.LogError(_log, "批次關閉 Agent Worker 時發生錯誤", ex);
            }
        }

        /// <summary>
        /// 同步 Dispose 所有 Worker
        /// </summary>
        public void Dispose()
        {
            try
            {
                DisposeAllAsync(CancellationToken.None).Wait();
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(_log, "AgentWorkerPool Dispose 時發生錯誤", ex);
            }
        }
    }
}
