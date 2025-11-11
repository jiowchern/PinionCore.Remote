using System;
using System.Threading.Tasks;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Server
{
    public static class ServiceExtensions
    {
        public static async System.Threading.Tasks.Task<IDisposable> ListenAsync(this IService service, params IListeningEndpoint[] listeningEndpoints)
        {
            var disposables = new System.Collections.Generic.List<IDisposable>();
            foreach (var listeningEndpoint in listeningEndpoints)
            {
                var disposable = await BeginListeningAsync(service, listeningEndpoint);
                disposables.Add(disposable);
            }

            return new Utility.DisposeAction(() =>
            {
                foreach (var disposable in disposables)
                {
                    disposable.Dispose();
                }
            });
        }

        private static async Task<IDisposable> BeginListeningAsync(IService service, IListeningEndpoint listeningEndpoint)
        {
            listeningEndpoint.StreamableEnterEvent += service.Join;
            listeningEndpoint.StreamableLeaveEvent += service.Leave;

            var result = await listeningEndpoint.ListenAsync();
            if (!result)
            {
                listeningEndpoint.StreamableEnterEvent -= service.Join;
                listeningEndpoint.StreamableLeaveEvent -= service.Leave;

                throw new Exception("Failed to listen bindable.");
            }

            IDisposable disposable = new Utility.DisposeAction(() =>
            {
                listeningEndpoint.Dispose();
                listeningEndpoint.StreamableEnterEvent -= service.Join;
                listeningEndpoint.StreamableLeaveEvent -= service.Leave;
            });

            return disposable;
        }
    }
}
