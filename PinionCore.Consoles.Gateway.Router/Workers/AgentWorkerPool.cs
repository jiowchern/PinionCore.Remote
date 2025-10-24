using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PinionCore.Consoles.Gateway.Router.Workers
{
    /// <summary>
    /// AgentWorkerPool 集中管理所有 AgentWorker，支援批次關閉
    /// </summary>
    public class AgentWorkerPool : IDisposable
    {
        private readonly List<AgentWorker> _workers = new List<AgentWorker>();
        private readonly object _lock = new object();

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
            catch
            {
                // 忽略 Dispose 錯誤
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

            // 平行關閉所有 Worker
            var disposeTasks = workersCopy.Select(w => Task.Run(() =>
            {
                try
                {
                    w.Dispose();
                }
                catch
                {
                    // 忽略個別 worker 的 Dispose 錯誤
                }
            }, cancellationToken));

            try
            {
                await Task.WhenAll(disposeTasks);
            }
            catch
            {
                // 忽略整體錯誤
            }
        }

        /// <summary>
        /// 同步 Dispose 所有 Worker
        /// </summary>
        public void Dispose()
        {
            DisposeAllAsync(CancellationToken.None).Wait();
        }
    }
}
