using System;

namespace PinionCore.Remote
{
    internal class GhostEventMoveHandler : ClientExchangeable
    {

        private readonly long _Ghost;

        private readonly IProtocol _Protocol;


        readonly IInternalSerializable _InternalSerializable;
        public GhostEventMoveHandler(long ghost, IProtocol protocol, IInternalSerializable internal_serializable)
        {
            _InternalSerializable = internal_serializable;
            this._Ghost = ghost;
            _Protocol = protocol;
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


        internal void Add(int event_id, long handler)
        {
            MemberMap map = _Protocol.GetMemberMap();


            var package = new PinionCore.Remote.Packages.PackageAddEvent();

            package.Entity = _Ghost;
            package.Event = event_id;
            package.Handler = handler;

            _ResponseEvent(ClientToServerOpCode.AddEvent, _InternalSerializable.Serialize(package));

        }



        internal void Remove(int event_id, long handler)
        {
            MemberMap map = _Protocol.GetMemberMap();


            var package = new PinionCore.Remote.Packages.PackageRemoveEvent();


            package.Entity = _Ghost;
            package.Event = event_id;
            package.Handler = handler;

            _ResponseEvent(ClientToServerOpCode.RemoveEvent, _InternalSerializable.Serialize(package));

        }


    }
}
