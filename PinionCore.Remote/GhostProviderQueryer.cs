﻿using System;
using PinionCore.Remote.ProviderHelper;

namespace PinionCore.Remote
{
    public class GhostProviderQueryer : ClientExchangeable
    {
        private readonly PingHandler _PingHandler;
        private readonly GhostsReturnValueHandler _ReturnValueHandler;
        private readonly GhostsOwner _GhostsOwner;
        private readonly GhostsHandler _GhostManager;
        private readonly GhostsResponer _GhostsResponser;
        private readonly ClientExchangeable[] ClientExchangeables;

        
        public float Ping => _PingHandler.PingTime;

        public event Action<string, string> ErrorMethodEvent
        {
            add { _ReturnValueHandler.ErrorMethodEvent += value; }
            remove { _ReturnValueHandler.ErrorMethodEvent -= value; }
        }
        public event Action<byte[], byte[]> VersionCodeErrorEvent
        {
            add { _GhostsResponser.VersionCodeErrorEvent += value; }
            remove { _GhostsResponser.VersionCodeErrorEvent -= value; }
        }
        public GhostProviderQueryer(
            IProtocol protocol,
            ISerializable serializer,
            IInternalSerializable internalSerializer,
            GhostsOwner ghosts_owner)
        {
            _PingHandler = new PingHandler();

            _ReturnValueHandler = new GhostsReturnValueHandler(serializer);
            _GhostsOwner = ghosts_owner;
            _GhostManager = new GhostsHandler(protocol, serializer, internalSerializer, _GhostsOwner, _ReturnValueHandler);

            _GhostsResponser = new GhostsResponer(internalSerializer, _GhostManager, _ReturnValueHandler, _PingHandler, _GhostsOwner, protocol);

            ClientExchangeables = new ClientExchangeable[]
            {
                _PingHandler,
                _GhostManager,
            };


        }

        event Action<ClientToServerOpCode, Memorys.Buffer> _ResponseEvent;
        event Action<ClientToServerOpCode, Memorys.Buffer> Exchangeable<ServerToClientOpCode, ClientToServerOpCode>.ResponseEvent
        {
            add
            {
                _ResponseEvent += value;
            }

            remove
            {
                _ResponseEvent -= value;
            }
        }

        public event Action<ClientToServerOpCode, Memorys.Buffer> ClientToServerEvent;




        public void Start()
        {
            foreach (ClientExchangeable exchangeable in ClientExchangeables)
            {
                exchangeable.ResponseEvent += _ResponseEvent;
            }

        }

        public void Stop()
        {
            foreach (ClientExchangeable exchangeable in ClientExchangeables)
            {
                exchangeable.ResponseEvent -= _ResponseEvent;
            }
            _GhostManager.ClearGhosts();
        }

        public INotifier<T> QueryProvider<T>()
        {
            return _GhostsOwner.QueryProvider<T>();
        }

        void Exchangeable<ServerToClientOpCode, ClientToServerOpCode>.Request(ServerToClientOpCode code, Memorys.Buffer args)
        {
            try
            {
                foreach (ClientExchangeable exchangeable in ClientExchangeables)
                {
                    exchangeable.Request(code, args);
                }
                _GhostsResponser.OnResponse(code, args);
            }
            catch (Exception e)
            {

                PinionCore.Utility.Log.Instance.WriteInfoImmediate($"GhostProviderQueryer Request error {e.ToString()} code:{code} ");
            }
            
        }


    }

    delegate System.Action<object> GetObjectAccesserMethod(IObjectAccessible accessible);

}
