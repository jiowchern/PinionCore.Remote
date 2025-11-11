using System;
using System.Threading;
using System.Threading.Tasks;
using PinionCore.Network;

namespace PinionCore.Remote.Soul
{
    public class ServiceUpdateLoop : Soul.IService
    {
        readonly SessionEngine _SyncService;
        readonly IService _Service;
        readonly IDisposable _Disposed;

        readonly Task _ThreadUpdater;
        readonly CancellationTokenSource Cancellation_;
        readonly SemaphoreSlim _UpdateSignal;
        readonly TimeSpan _MaxWaitInterval = TimeSpan.FromMilliseconds(16);

        public ServiceUpdateLoop(SessionEngine syncService)
        {
            if (syncService == null)
            {
                throw new ArgumentNullException(nameof(syncService));
            }

            _SyncService = syncService;
            _Service = syncService;
            _Disposed = _SyncService;

            Cancellation_ = new CancellationTokenSource();
            _UpdateSignal = new SemaphoreSlim(0);
            
            _RequestUpdate();

            _ThreadUpdater = Task.Factory.StartNew(() => _Update(), Cancellation_.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void _Update()
        {
            try
            {
                while (true)
                {
                    var token = Cancellation_.Token;
                    token.ThrowIfCancellationRequested();

                    try
                    {
                        _UpdateSignal.Wait(_MaxWaitInterval, token);
                        while (_UpdateSignal.Wait(0))
                        {
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    _SyncService.Update();
                }
            }
            catch (OperationCanceledException)
            {
                // graceful shutdown
            }
        }

        void _RequestUpdate()
        {
            if (Cancellation_.IsCancellationRequested)
            {
                return;
            }

            try
            {
                _UpdateSignal.Release();
            }
            catch (ObjectDisposedException)
            {
                // shutting down, ignore
            }
        }

        void IDisposable.Dispose()
        {
            Cancellation_.Cancel();
            _RequestUpdate();

            try
            {
                _ThreadUpdater.Wait();
            }
            finally
            {                
                _UpdateSignal.Dispose();
                _Disposed.Dispose();
                Cancellation_.Dispose();
            }
        }

        void IService.Join(IStreamable user)
        {
            _Service.Join(user);
            _RequestUpdate();
        }

        void IService.Leave(IStreamable user)
        {
            _Service.Leave(user);
            _RequestUpdate();
        }
    }
}

