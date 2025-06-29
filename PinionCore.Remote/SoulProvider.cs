using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PinionCore.Remote
{


    public class SoulProvider : IDisposable, IBinder
    {
        private readonly IdLandlord _IdLandlord;
        private readonly IRequestQueue _Peer;
        private readonly IResponseQueue _Queue;
        private readonly IProtocol _Protocol;
        private readonly EventProvider _EventProvider;
        private readonly ConcurrentDictionary<long, SoulProxy> _Souls;
        private readonly ISerializable _Serializer;
        private readonly IInternalSerializable _InternalSerializable;

        private readonly SoulBindHandler _bindHandler;
        private readonly SoulMethodHandler _methodHandler;
        private readonly SoulEventHandler _eventHandler;
        private readonly SoulPropertyHandler _propertyHandler;

        public SoulProvider(
            IRequestQueue peer,
            IResponseQueue queue,
            IProtocol protocol,
            ISerializable serializer,
            IInternalSerializable internalSerializable)
        {
            _IdLandlord = new IdLandlord();
            _Peer = peer;
            _Queue = queue;
            _Protocol = protocol;
            _Serializer = serializer;
            _InternalSerializable = internalSerializable;

            _EventProvider = protocol.GetEventProvider();
            _Souls = new ConcurrentDictionary<long, SoulProxy>();
        

            _bindHandler = new SoulBindHandler(_IdLandlord, _Queue, _Protocol, _Souls, _Serializer, _InternalSerializable, _EventProvider);
            _methodHandler = new SoulMethodHandler(_Peer, _Queue, _Protocol, _Souls,  _Serializer, _InternalSerializable);
            _eventHandler = new SoulEventHandler(_Queue, _Protocol, _Souls, _EventProvider, _Serializer, _InternalSerializable);
            _propertyHandler = new SoulPropertyHandler(_Queue, _Protocol, _Souls, _Serializer, _InternalSerializable);
        }

        public void Dispose()
        {
            _methodHandler.Dispose();
        }

        ISoul IBinder.Return<TSoul>(TSoul soul)
        {
            return _bindHandler.Return(soul);
        }

        ISoul IBinder.Bind<TSoul>(TSoul soul)
        {
            return _bindHandler.Bind(soul);
        }

        void IBinder.Unbind(ISoul soul)
        {
            _bindHandler.Unbind(soul);
        }

        public void AddEvent(long entity, int @event, long handler)
        {
            _eventHandler.AddEvent(entity, @event, handler);
        }

        public void RemoveEvent(long entity, int @event, long handler)
        {
            _eventHandler.RemoveEvent(entity, @event, handler);
        }

        public void SetPropertyDone(long entityId, int property)
        {
            _propertyHandler.SetPropertyDone(entityId, property);
        }

        public void Unbind(long entityId)
        {
            SoulProxy soul;
            _Souls.TryGetValue(entityId, out soul);
            _bindHandler.Unbind(soul);

        }


        // 其他需要實現的接口方法或事件處理可以在這裡添加
    }

}
