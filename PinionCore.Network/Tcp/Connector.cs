using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PinionCore.Network.Tcp
{
    public sealed class Connector
    {
        public async Task<Peer> ConnectAsync(EndPoint remoteEP, TimeSpan timeout = default, CancellationToken ct = default)
        {
            if (timeout == default)
                timeout = TimeSpan.FromMinutes(1);

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                // 以 Begin/End 包成 Task，避免不同平台/版本的 ConnectAsync 回傳 ValueTask 造成型別不相容
                var connectTask = Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, remoteEP, null);

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(timeout);

                var completed = await Task.WhenAny(connectTask, Task.Delay(Timeout.Infinite, cts.Token))
                                          .ConfigureAwait(false);

                if (completed != connectTask)
                {
                    // 逾時/取消：主動處理 socket，避免殭屍連線
                    try { socket.Dispose(); } catch { /* ignore */ }

                    ct.ThrowIfCancellationRequested();
                    throw new TimeoutException($"Connect to {remoteEP} timed out after {timeout}.");
                }

                // 若完成的是 connectTask，此 await 幾乎是同步的
                await connectTask.ConfigureAwait(false);
                return new Peer(socket);
            }
            catch (SocketException sex)
            {
                socket.Dispose();
                throw new Exception($"Connect to {remoteEP} failed (SocketError: {sex.SocketErrorCode}).", sex);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }
    }
}
