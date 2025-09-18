using System;
using PinionCore.Memorys;
using PinionCore.Network;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway.Sessions
{
    enum OpCodeClientToServer : byte
    {
        None = 0,
        Join = 1,
        Leave = 2,
        Message = 3
    }


    class SessionChannel
    {
        readonly GatewaySessionListener _Listener;
        public readonly IListenable Listenable;

        readonly System.Action _Disposes;
        readonly Stream _Stream;
        readonly Serializer _Serializer;
        readonly IPool _Pool;
        public SessionChannel(Memorys.IPool pool)
        {
            var serializer = new Serializer(pool);
            var stream = new PinionCore.Network.Stream();
            var reader = new PinionCore.Network.PackageReader(stream, pool);
            var sender = new PinionCore.Network.PackageSender(stream, pool);
            _Pool = pool;
            _Stream = stream;
            _Serializer = serializer;
            _Listener = new GatewaySessionListener(reader, sender, serializer);
            Listenable = _Listener;
            _Listener.Start();

            _Disposes = () =>
            {
                var listenerDispose = _Listener as IDisposable;
                listenerDispose.Dispose();
            };
        }

        public GatewaySessionConnector SpawnConnector(IStreamable streamable)
        {
            var stream = new PinionCore.Network.ReverseStream(_Stream);
            var reader = new PinionCore.Network.PackageReader(stream, _Pool);
            var sender = new PinionCore.Network.PackageSender(stream, _Pool);
            var connector = new GatewaySessionConnector(reader, sender, _Serializer, _Pool);
            connector.Start();
            return connector;
        }

        public void Dispose()
        {
            _Disposes();
        }
    }
}
