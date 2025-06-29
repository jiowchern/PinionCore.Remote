using System;
using System.Threading;
using System.Threading.Tasks;

namespace PinionCore.Remote.Soul
{
    public class AsyncService : Soul.IService
    {



        readonly SyncService _SyncService;
        readonly IDisposable _Disposed;

        readonly System.Threading.Tasks.Task _ThreadUpdater;
        readonly CancellationTokenSource Cancellation_;

        public AsyncService(SyncService syncService)
        {
            _SyncService = syncService;
            _Disposed = _SyncService;

            Cancellation_ = new CancellationTokenSource();
            _ThreadUpdater = Task.Factory.StartNew(() => _Update(), TaskCreationOptions.LongRunning);
        }

        private void _Update()
        {
            var ar = new PinionCore.Utility.AutoPowerRegulator(new PinionCore.Utility.PowerRegulator());
            while (!Cancellation_.IsCancellationRequested)
            {
                _SyncService.Update();
                ar.Operate(new CancellationTokenSource());
            }
        }

        void IDisposable.Dispose()
        {
            Cancellation_.Cancel();
            _ThreadUpdater.Wait();
            _Disposed.Dispose();
            Cancellation_.Dispose();
        }

    }
}

