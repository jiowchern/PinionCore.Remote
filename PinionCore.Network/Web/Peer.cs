using System;
using System.Net.WebSockets;

namespace PinionCore.Network.Web
{
    using System.Threading.Tasks;
    using PinionCore.Remote;
    public class Peer : IStreamable, IDisposable
    {
        private readonly WebSocket _Socket;
        readonly System.Threading.CancellationTokenSource _CancelSource;

        public Peer(WebSocket socket)
        {
            _Socket = socket;

            _CancelSource = new System.Threading.CancellationTokenSource();



        }
        public event System.Action<WebSocketState> ErrorEvent;
        void IDisposable.Dispose()
        {
            _CancelSource.Cancel();
        }

        IAwaitableSource<int> IStreamable.Receive(byte[] buffer, int offset, int count)
        {
            if (_Socket.State == WebSocketState.Aborted)
            {
                ErrorEvent?.Invoke(_Socket.State);
                return 0.ToWaitableValue();
            }


            var segment = new ArraySegment<byte>(buffer, offset, count);
            return _Socket.ReceiveAsync(segment, _CancelSource.Token).ContinueWith<int>((t) =>
            {
                try
                {

                    WebSocketReceiveResult r = t.Result;
                    return r.Count;
                }
                catch (Exception e)
                {
                    PinionCore.Utility.Log.Instance.WriteInfo($"websocket receive error state:{_Socket.State} ,{e.ToString()}.");

                    ErrorEvent?.Invoke(_Socket.State);


                }
                return 0;
            }).ToWaitableValue();
        }

        IAwaitableSource<int> IStreamable.Send(byte[] buffer, int offset, int count)
        {
            if (_Socket.State == WebSocketState.Aborted)
            {
                ErrorEvent?.Invoke(_Socket.State);
                return 0.ToWaitableValue();
            }
            var arraySegment = new ArraySegment<byte>(buffer, offset, count);
            return _Socket.SendAsync(arraySegment, WebSocketMessageType.Binary, true, _CancelSource.Token).ContinueWith((t) =>
            {
                return count;
            }).ToWaitableValue();


        }
        public Task DisconnectAsync() => DisconnectAsync(TimeSpan.FromSeconds(5));

        public async Task DisconnectAsync(TimeSpan timeout)
        {
            if (_Socket.State == WebSocketState.Closed || _Socket.State == WebSocketState.Aborted)
                return;

            using var cts = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(_CancelSource.Token);
            cts.CancelAfter(timeout);

            try
            {
                await _Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "close", cts.Token)
                              .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // 對端未回應 close，強制中止
                _Socket.Abort();
            }
            catch (WebSocketException)
            {
                // 網路錯誤等情況，保底中止
                _Socket.Abort();
            }
        }
    }
}
