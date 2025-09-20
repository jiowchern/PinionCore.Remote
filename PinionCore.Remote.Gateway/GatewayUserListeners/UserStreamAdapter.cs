using System;
using PinionCore.Network;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.GatewayUserListeners
{
    public sealed class UserStreamAdapter : IStreamable, IDisposable
    {
        private readonly IUser _user;
        private readonly Stream _stream;
        private readonly IStreamable _streamView;
        private bool _disposed;

        public UserStreamAdapter(IUser user)
        {
            _user = user ?? throw new ArgumentNullException(nameof(user));
            _stream = new Stream();
            _streamView = _stream;
            _user.ResponseEvent += _OnResponse;
        }

        public IAwaitableSource<int> Receive(byte[] buffer, int offset, int count)
        {
            return _streamView.Receive(buffer, offset, count);
        }

        public IAwaitableSource<int> Send(byte[] buffer, int offset, int count)
        {
            if (count <= 0)
            {
                return 0.ToWaitableValue();
            }

            var payload = new byte[count];
            Array.Copy(buffer, offset, payload, 0, count);

            _user.Request(payload);
            return count.ToWaitableValue();
        }

        private void _OnResponse(byte[] payload)
        {
            if (payload == null)
            {
                return;
            }

            _stream.Push(payload, 0, payload.Length);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _user.ResponseEvent -= _OnResponse;
        }
    }
}
