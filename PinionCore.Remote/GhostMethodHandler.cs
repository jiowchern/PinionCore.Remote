﻿using System;
using System.Linq;
using System.Reflection;
using PinionCore.Remote.ProviderHelper;

namespace PinionCore.Remote
{
    internal class GhostMethodHandler : ClientExchangeable
    {

        private readonly IProtocol _Protocol;
        private readonly ISerializable _Serializable;
        private readonly GhostsReturnValueHandler _ReturnValueQueue;

        private readonly long _Ghost;


        readonly IInternalSerializable _InternalSerializable;
        private readonly ServerToClientOpCode[] _Empty;

        public GhostMethodHandler(long ghost,
            GhostsReturnValueHandler return_value_queue,
            IProtocol protocol,
            ISerializable serializable,
            IInternalSerializable internal_serializable)
        {
            _InternalSerializable = internal_serializable;
            _Ghost = ghost;
            _ReturnValueQueue = return_value_queue;
            _Protocol = protocol;
            this._Serializable = serializable;
            _Empty = new ServerToClientOpCode[0];
        }
        void Exchangeable<ServerToClientOpCode, ClientToServerOpCode>.Request(ServerToClientOpCode code, Memorys.Buffer args)
        {
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

        public void Run(int method_id, object[] args, IValue return_value)
        {
            MemberMap map = _Protocol.GetMemberMap();
            ISerializable serialize = _Serializable;
            var info = map.GetMethod(method_id);
            if (info == null)
            {
                PinionCore.Utility.Log.Instance.WriteInfoImmediate($"method {info.Name} not found");
                return;
            }
            var package = new PinionCore.Remote.Packages.PackageCallMethod();



            package.EntityId = _Ghost;
            package.MethodId = method_id;

            package.MethodParams = args.Zip(info.GetParameters(), (arg, par) => serialize.Serialize(par.ParameterType, arg).ToArray()).ToArray();


            if (return_value != null)
                package.ReturnId = _ReturnValueQueue.PushReturnValue(return_value);

            _ResponseEvent(ClientToServerOpCode.CallMethod, _InternalSerializable.Serialize(package));
        }


    }
}
