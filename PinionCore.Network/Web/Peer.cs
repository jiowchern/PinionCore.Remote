using System;
using System.Net.WebSockets;

namespace PinionCore.Network.Web
{
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

        IWaitableValue<int> IStreamable.Receive(byte[] buffer, int offset, int count)
        {
            if (_Socket.State == WebSocketState.Aborted)
            {
                ErrorEvent(_Socket.State);
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

                    ErrorEvent(_Socket.State);


                }
                return 0;
            }).ToWaitableValue();
        }

        IWaitableValue<int> IStreamable.Send(byte[] buffer, int offset, int count)
        {
            if (_Socket.State == WebSocketState.Aborted)
            {
                ErrorEvent(_Socket.State);
                return 0.ToWaitableValue();
            }
            var arraySegment = new ArraySegment<byte>(buffer, offset, count);
            return _Socket.SendAsync(arraySegment, WebSocketMessageType.Binary, true, _CancelSource.Token).ContinueWith((t) =>
            {
                return count;
            }).ToWaitableValue();


        }
    }
}
