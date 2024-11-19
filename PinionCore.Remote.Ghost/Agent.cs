using System;
using PinionCore.Memorys;
using PinionCore.Network;
using PinionCore.Remote.ProviderHelper;
using PinionCore.Utility;



namespace PinionCore.Remote.Ghost
{
    public class Agent : IAgent
    {

        private readonly GhostProviderQueryer _GhostProvider;
        private readonly GhostsOwner _GhostsOwner;
        private readonly IPool _Pool;
        private readonly IInternalSerializable _InternalSerializer;

        private System.Action _GhostSerializerUpdater;
        private System.Action _GhostSerializerStop;
        private float _Ping
        {
            get { return _GhostProvider.Ping; }
        }

        public Agent(IProtocol protocol, ISerializable serializable, IInternalSerializable internal_serializable, PinionCore.Memorys.IPool pool)
        {
            _InternalSerializer = internal_serializable;
            _Pool = pool;
            _GhostsOwner = new GhostsOwner(protocol);

            _GhostProvider = new GhostProviderQueryer(protocol, serializable, internal_serializable, _GhostsOwner);
            _GhostSerializerUpdater = () => { };
            _GhostSerializerStop = () => { };

            Singleton<Log>.Instance.WriteInfo("Agent Launch.");            
            _ExceptionEvent += (e) => { };
        }

        void IAgent.Update()
        {
            _GhostSerializerUpdater();

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

            _GhostSerializerStop =
            () =>
            {
                var senderDispose = sender as IDisposable;
                senderDispose.Dispose();
                ghostSerializer.ErrorEvent -= _ExceptionEvent;
                ghostSerializer.Stop();
                serverExchangeable.ResponseEvent -= clientExchangeable.Request;
                clientExchangeable.ResponseEvent -= serverExchangeable.Request;
            };

            _GhostProvider.Start();
            ghostSerializer.Start();


            _GhostSerializerUpdater = ghostSerializer.Update;


        }
        public void Disable()
        {

            _GhostSerializerUpdater = () => { };

            _GhostSerializerStop();
            _GhostSerializerStop = () => { };
            _GhostsOwner.ClearProviders();
            _GhostProvider.Stop();
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
