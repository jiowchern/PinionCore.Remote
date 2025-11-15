using System;

using System.Threading.Tasks;
using PinionCore.Network;
using PinionCore.Remote.Client;
using PinionCore.Remote.Server;
using PinionCore.Remote.Soul;


namespace PinionCore.Remote.Standalone
{
    public class ListeningEndpoint : PinionCore.Remote.Server.IListeningEndpoint, PinionCore.Remote.Client.IConnectingEndpoint
    {
        event Action<IStreamable> IListenable.StreamableEnterEvent
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        event Action<IStreamable> IListenable.StreamableLeaveEvent
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        Task<IStreamable> IConnectingEndpoint.ConnectAsync()
        {
            throw new NotImplementedException();
        }

        void IDisposable.Dispose()
        {
            throw new NotImplementedException();
        }

        Task<Exception> IListeningEndpoint.ListenAsync()
        {
            throw new NotImplementedException();
        }
    }
}
