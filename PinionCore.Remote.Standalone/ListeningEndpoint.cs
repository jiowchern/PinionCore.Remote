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
        private readonly Depot<IStreamable> _ConnectedStreams;
        private bool _Disposed;

        public ListeningEndpoint()
        {
            _ConnectedStreams = new Depot<IStreamable>();
        }

        event Action<IStreamable> IListenable.StreamableEnterEvent
        {
            add { _ConnectedStreams.Notifier.Supply += value; }
            remove { _ConnectedStreams.Notifier.Supply -= value; }
        }

        event Action<IStreamable> IListenable.StreamableLeaveEvent
        {
            add { _ConnectedStreams.Notifier.Unsupply += value; }
            remove { _ConnectedStreams.Notifier.Unsupply -= value; }
        }

        Task<IStreamable> IConnectingEndpoint.ConnectAsync()
        {
            if (_Disposed)
                throw new ObjectDisposedException(nameof(ListeningEndpoint));

            // 創建一對雙向 stream
            var serverStream = new Stream();
            var clientStream = new ClientStreamWrapper(serverStream, this);

            // 將伺服器端的 stream 添加到集合，這會觸發 StreamableEnterEvent
            _ConnectedStreams.Items.Add(serverStream);

            // 返回客戶端的 stream（包裝版本，以便在 dispose 時通知伺服器）
            return Task.FromResult<IStreamable>(clientStream);
        }

        void IDisposable.Dispose()
        {
            if (_Disposed)
                return;

            _Disposed = true;
            _ConnectedStreams.Items.Clear();
        }

        Task<Exception> IListeningEndpoint.ListenAsync()
        {
            if (_Disposed)
                return Task.FromResult<Exception>(new ObjectDisposedException(nameof(ListeningEndpoint)));

            // Standalone 模式不需要實際監聽，直接返回 null 表示成功
            return Task.FromResult<Exception>(null);
        }

        private void OnClientStreamDisposed(Stream serverStream)
        {
            if (!_Disposed)
            {
                _ConnectedStreams.Items.Remove(serverStream);
            }
        }

        private class ClientStreamWrapper : IStreamable
        {
            private readonly Stream _ServerStream;
            private readonly IStreamable _ClientStream;
            private readonly ListeningEndpoint _Endpoint;
            private bool _Disposed;

            public ClientStreamWrapper(Stream serverStream, ListeningEndpoint endpoint)
            {
                _ServerStream = serverStream;
                _ClientStream = new ReverseStream(serverStream);
                _Endpoint = endpoint;
            }

            public IAwaitableSource<int> Receive(byte[] buffer, int offset, int count)
            {
                return _ClientStream.Receive(buffer, offset, count);
            }

            public IAwaitableSource<int> Send(byte[] buffer, int offset, int count)
            {
                return _ClientStream.Send(buffer, offset, count);
            }

            public void Dispose()
            {
                if (_Disposed)
                    return;

                _Disposed = true;

                // 通知 endpoint 客戶端已斷開
                _Endpoint.OnClientStreamDisposed(_ServerStream);

                // 釋放資源
                _Dispose(_ClientStream);
                _Dispose(_ServerStream);
            }

            void _Dispose(IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
