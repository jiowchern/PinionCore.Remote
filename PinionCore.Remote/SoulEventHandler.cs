using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using PinionCore.Remote.Packages;

namespace PinionCore.Remote
{

    public class SoulEventHandler
    {
        private readonly IResponseQueue _Queue;
        private readonly IProtocol _Protocol;
        private readonly ConcurrentDictionary<long, SoulProxy> _Souls;
        private readonly EventProvider _EventProvider;
        private readonly ISerializable _Serializer;
        private readonly IInternalSerializable _InternalSerializable;

        public SoulEventHandler(
            IResponseQueue queue,
            IProtocol protocol,
            ConcurrentDictionary<long, SoulProxy> souls,
            EventProvider eventProvider,
            ISerializable serializer,
            IInternalSerializable internalSerializable)
        {
            _Queue = queue;
            _Protocol = protocol;
            _Souls = souls;
            _EventProvider = eventProvider;
            _Serializer = serializer;
            _InternalSerializable = internalSerializable;
        }

        public void AddEvent(long entityId, int eventId, long handlerId)
        {
            if (!_Souls.TryGetValue(entityId, out SoulProxy soul))
            {
                throw new Exception($"AddEvent Soul not found entity_id:{entityId}");
            }


            EventInfo eventInfo = _Protocol.GetMemberMap().GetEvent(eventId);
            if (eventInfo == null || !soul.Is(eventInfo.DeclaringType))
            {
                throw new Exception($"AddEvent Event not found event_id:{eventId}");
            }


            Delegate del = _BuildDelegate(eventId, soul.Id, handlerId, _InvokeEvent);
            var handler = new SoulProxyEventHandler(soul.ObjectInstance, del, eventInfo, handlerId);
            soul.AddEvent(handler);
        }

        public void RemoveEvent(long entityId, int eventId, long handlerId)
        {
            if (!_Souls.TryGetValue(entityId, out SoulProxy soul))
                throw new Exception($"RemoveEventSoul not found entity_id:{entityId}");

            EventInfo eventInfo = _Protocol.GetMemberMap().GetEvent(eventId);
            if (eventInfo == null || !soul.Is(eventInfo.DeclaringType))
                throw new Exception($"RemoveEvent Event not found event_id:{eventId}");

            soul.RemoveEvent(eventInfo, handlerId);
        }

        private void _InvokeEvent(long entityId, int eventId, long handlerId, object[] args)
        {
            EventInfo info = _Protocol.GetMemberMap().GetEvent(eventId);
            var package = new PackageInvokeEvent
            {
                EntityId = entityId,
                Event = eventId,
                HandlerId = handlerId,
                EventParams = args.Zip(info.EventHandlerType.GetGenericArguments(), (arg, par) => _Serializer.Serialize(par, arg).ToArray()).ToArray()
            };
            _Queue.Push(ServerToClientOpCode.InvokeEvent, _InternalSerializable.Serialize(package));
        }

        private Delegate _BuildDelegate(int event_id, long entityId, long handlerId, InvokeEventCallabck invokeEvent)
        {
            MemberMap map = _Protocol.GetMemberMap();
            EventInfo info = map.GetEvent(event_id);
            IEventProxyCreater eventCreator = _EventProvider.Find(info);


            return eventCreator.Create(entityId, event_id, handlerId, invokeEvent);
        }
    }

}

