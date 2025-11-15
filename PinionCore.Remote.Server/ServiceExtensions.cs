using System;
using System.Threading.Tasks;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Server
{
    public static class ServiceExtensions
    {
        public struct ErrorInfo
        {
            public IListeningEndpoint ListeningEndpoint;
            public Exception Exception;
        }
        public static async System.Threading.Tasks.Task<(IDisposable, ErrorInfo[])> ListenAsync(this IService service, params IListeningEndpoint[] listeningEndpoints)
        {
            var multi = new StreamableHub();
            PinionCore.Remote.Soul.IListenable multiListener = multi;
            multiListener.StreamableLeaveEvent += service.Leave;
            multiListener.StreamableEnterEvent += service.Join;

            var disposables = new System.Collections.Generic.List<IDisposable>();
            var errorInfos = new System.Collections.Generic.List<ErrorInfo>();
            foreach (var listeningEndpoint in listeningEndpoints)
            {
                var (disposable, exception) = await BeginListeningAsync(multi, listeningEndpoint);
                if (exception != null)
                {
                    errorInfos.Add(new ErrorInfo
                    {
                        ListeningEndpoint = listeningEndpoint,
                        Exception = exception
                    });

                    continue;
                }
                disposables.Add(disposable);
            }

            var action = new Utility.DisposeAction(() =>
            {

                multiListener.StreamableLeaveEvent -= service.Leave;
                multiListener.StreamableEnterEvent -= service.Join;

                foreach (var disposable in disposables)
                {
                    disposable.Dispose();
                }
            });
            return (action, errorInfos.ToArray());
        }

        private static async Task<(IDisposable,Exception)> BeginListeningAsync(StreamableHub multi, IListeningEndpoint listeningEndpoint)
        {
            
            
            var exception = await listeningEndpoint.ListenAsync();
            if (exception != null)
            {
            
                return (null, exception);
            }
            listeningEndpoint.StreamableEnterEvent += multi.Add;
            listeningEndpoint.StreamableLeaveEvent += multi.Remove;


            IDisposable disposable = new Utility.DisposeAction(() =>
            {
                listeningEndpoint.StreamableEnterEvent -= multi.Add;
                listeningEndpoint.StreamableLeaveEvent -= multi.Remove;
                listeningEndpoint.Dispose();                
            });

            return (disposable, null);
        }
    }
}
