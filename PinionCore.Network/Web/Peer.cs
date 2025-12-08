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

        IAwaitableSource<int> IStreamable.Receive(byte[] buffer, int offset, int count, System.Threading.CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return 0.ToWaitableValue();

            if (_Socket.State == WebSocketState.Aborted)
            {
                ErrorEvent?.Invoke(_Socket.State);
                return 0.ToWaitableValue();
            }

            var linkedTokenSource = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(_CancelSource.Token, token);
            var segment = new ArraySegment<byte>(buffer, offset, count);
            var task = _Socket.ReceiveAsync(segment, linkedTokenSource.Token).ContinueWith<int>((t) =>
            {
                try
                {
                    if (t.IsCanceled)
                        return 0;

                    WebSocketReceiveResult r = t.Result;
                    return r.Count;
                }
                catch (Exception e)
                {
                    PinionCore.Utility.Log.Instance.WriteInfo($"websocket receive error state:{_Socket.State} ,{e.ToString()}.");

                    ErrorEvent?.Invoke(_Socket.State);
                }
                finally
                {
                    linkedTokenSource.Dispose();
                }
                return 0;
            }, TaskScheduler.Default);
            return task.ToWaitableValue();
        }

        IAwaitableSource<int> IStreamable.Send(byte[] buffer, int offset, int count, System.Threading.CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return 0.ToWaitableValue();

            if (_Socket.State == WebSocketState.Aborted)
            {
                ErrorEvent?.Invoke(_Socket.State);
                return 0.ToWaitableValue();
            }

            var linkedTokenSource = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(_CancelSource.Token, token);
            var arraySegment = new ArraySegment<byte>(buffer, offset, count);
            var task = _Socket.SendAsync(arraySegment, WebSocketMessageType.Binary, true, linkedTokenSource.Token).ContinueWith((t) =>
            {
                try
                {
                    if (t.IsCanceled)
                        return 0;
                    return count;
                }
                finally
                {
                    linkedTokenSource.Dispose();
                }
            }, TaskScheduler.Default);
            return task.ToWaitableValue();


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
