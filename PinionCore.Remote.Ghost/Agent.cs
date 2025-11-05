using System;
using PinionCore.Memorys;
using PinionCore.Network;
using PinionCore.Remote.ProviderHelper;



namespace PinionCore.Remote.Ghost
{
    public class Agent : IAgent
    {

        private readonly GhostProviderQueryer _GhostProvider;
        private readonly GhostsOwner _GhostsOwner;
        private readonly IPool _Pool;
        private readonly IInternalSerializable _InternalSerializer;

        private System.Action _GhostProviderUpdater;
        private System.Action _GhostSerializerUpdater;
        private System.Action _Disables;
        private float _Ping
        {
            get { return _GhostProvider.Ping; }
        }
       

        public Agent(IProtocol protocol)
            : this(protocol, new PinionCore.Remote.Serializer(protocol.SerializeTypes), new PinionCore.Remote.InternalSerializer(), PinionCore.Memorys.PoolProvider.Shared)
        {
        }
        public Agent(IProtocol protocol, ISerializable serializable, IInternalSerializable internal_serializable, PinionCore.Memorys.IPool pool)
        {
            _InternalSerializer = internal_serializable;
            _Pool = pool;
            _GhostsOwner = new GhostsOwner(protocol);

            _GhostProvider = new GhostProviderQueryer(protocol, serializable, internal_serializable, _GhostsOwner);
            _GhostSerializerUpdater = () => { };
            _GhostProviderUpdater = () => { };
            _Disables = () => { };

            _ExceptionEvent += (e) => { };
        }

        void IAgent.HandlePackets()
        {
            _GhostSerializerUpdater();

        }
        void IAgent.HandleMessage()
        {
            _GhostProviderUpdater();
        }

        public void Enable(IStreamable streamable)
        {
            var sender = new PackageSender(streamable, _Pool);
            var reader = new PackageReader(streamable, _Pool);
            var ghostSerializer = new GhostSerializer(reader, sender, _InternalSerializer);
            ServerExchangeable serverExchangeable = ghostSerializer;
            ClientExchangeable clientExchangeable = _GhostProvider;
            ghostSerializer.ErrorEvent += _ExceptionEvent;
            serverExchangeable.ResponseEvent += clientExchangeable.Request;
            clientExchangeable.ResponseEvent += serverExchangeable.Request;

            _Disables =
            () =>
            {
                _GhostProviderUpdater = () => { };
                _GhostSerializerUpdater = () => { };

                ghostSerializer.ErrorEvent -= _ExceptionEvent;
                ghostSerializer.Stop();

                IDisposable streamableDispose = streamable;
                streamableDispose.Dispose();

                IDisposable senderDispose = sender;
                senderDispose.Dispose();

                IDisposable readerDispose = reader;
                readerDispose.Dispose();

                serverExchangeable.ResponseEvent -= clientExchangeable.Request;
                clientExchangeable.ResponseEvent -= serverExchangeable.Request;

                _GhostsOwner.ClearProviders();
                _GhostProvider.Stop();
            };

            _GhostProvider.Start();
            ghostSerializer.Start();


            _GhostSerializerUpdater = ghostSerializer.Update;
            _GhostProviderUpdater = _GhostProvider.Update;



        }
        public void Disable()
        {


            _Disables();
            _Disables = () => { };

        }
        INotifier<T> INotifierQueryable.QueryNotifier<T>()
        {
            return _GhostsOwner.QueryProvider<T>();
        }

        float IAgent.Ping
        {
            get { return _Ping; }
        }


        event Action<byte[], byte[]> IAgent.VersionCodeErrorEvent
        {
            add { _GhostProvider.VersionCodeErrorEvent += value; }
            remove { _GhostProvider.VersionCodeErrorEvent -= value; }

        }


        event Action<string, string> IAgent.ErrorMethodEvent
        {
            add { _GhostProvider.ErrorMethodEvent += value; }
            remove { _GhostProvider.ErrorMethodEvent -= value; }
        }

        event Action<Exception> _ExceptionEvent;
        event Action<Exception> IAgent.ExceptionEvent
        {
            add
            {
                _ExceptionEvent += value;
            }

            remove
            {
                _ExceptionEvent -= value;
            }
        }
    }
}
