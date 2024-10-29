﻿using System.Threading;
using System.Threading.Tasks;
using PinionCore.Utility;

namespace PinionCore.Remote
{
    public class ThreadUpdater
    {
        private readonly System.Action _Updater;

        private CancellationTokenSource _Cancel;
        private Task _Task;

        public ThreadUpdater(System.Action updater)
        {
            _Updater = updater;
        }

        void _Update(CancellationToken token)
        {
            var regulator = new AutoPowerRegulator(new PowerRegulator());

            while (!token.IsCancellationRequested)
            {
                _Updater();
                regulator.Operate();
            }

        }
        public void Start()
        {

            _Cancel = new CancellationTokenSource();

            _Task = System.Threading.Tasks.Task.Run(() => _Update(_Cancel.Token), _Cancel.Token);


        }

        public void Stop()
        {
            _Cancel.Cancel();
            _Task.Wait();
            _Cancel.Dispose();
        }
    }
}
