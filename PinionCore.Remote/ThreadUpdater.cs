using System.Threading;
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

        void _Update(CancellationTokenSource source)
        {
            var regulator = new AutoPowerRegulator(new PowerRegulator());

            while (!source.IsCancellationRequested)
            {
                _Updater();
                regulator.Operate(source);
            }

        }
        public void Start()
        {
            _Cancel = new CancellationTokenSource();

            _Task = System.Threading.Tasks.Task.Run(() => _Update(_Cancel));
        }

        public void Stop()
        {
            _Cancel.Cancel();
            try
            {
                _Task.Wait();
            }
            catch (System.AggregateException)
            {

                
            }
            catch(System.Threading.Tasks.TaskCanceledException)
            {

            }
            _Cancel.Dispose();
        }
    }
}
