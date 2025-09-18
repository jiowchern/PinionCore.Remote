﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PinionCore.Remote
{
    public class SoulProxy : ISoul
    {

        public readonly long Id;

        public readonly object ObjectInstance;

        public readonly Type ObjectType;

        public readonly MethodInfo[] MethodInfos;
        public readonly PropertyInfo[] PropertyInfos;


        readonly List<SoulProxyEventHandler> _EventHandlers;
        readonly List<PropertyUpdater> _PropertyUpdaters;
        readonly List<NotifierUpdater> _TypeObjectNotifiables;
        readonly System.Action _Dispose;

        public delegate ISoul OnSupplyPropertySoul(long soul_id, int property_id, TypeObject type_object);
        public delegate void OnUnsupplyPropertySoul(long soul_id, int property_id, long property_soul_id);
        public event OnSupplyPropertySoul SupplySoulEvent;
        public event OnUnsupplyPropertySoul UnsupplySoulEvent;

        public event System.Action<long, IPropertyIdValue> PropertyChangedEvent;

        public readonly int InterfaceId;

        long ISoul.Id => Id;
        object ISoul.Instance => ObjectInstance;


        public SoulProxy(long id, int interface_id, Type object_type, object object_instance)
        {

            MethodInfos = _GetInterfaces(object_type).SelectMany(i => i.GetMethods()).Union(new MethodInfo[0]).ToArray();

            PropertyInfos = _GetInterfaces(object_type).SelectMany(s => s.GetProperties(BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public)).Union(new PropertyInfo[0]).ToArray();
            ObjectInstance = object_instance;
            ObjectType = object_type;
            InterfaceId = interface_id;
            Id = id;


            _EventHandlers = new List<SoulProxyEventHandler>();
            _TypeObjectNotifiables = new List<NotifierUpdater>();
            _PropertyUpdaters = new List<PropertyUpdater>();

            _Dispose = () =>
            {

                _Unregister(_PropertyUpdaters);
                _Unregister(_TypeObjectNotifiables);

                lock (_EventHandlers)
                {
                    foreach (SoulProxyEventHandler eventHandler in _EventHandlers)
                    {
                        eventHandler.Release();

                    }
                    _EventHandlers.Clear();
                }

                foreach (PropertyUpdater pu in _PropertyUpdaters)
                {
                    pu.Release();
                }
                _PropertyUpdaters.Clear();


                _TypeObjectNotifiables.Clear();
            };
        }
        public void Initial(IReadOnlyDictionary<PropertyInfo, int> property_ids)
        {
            _PropertyUpdaters.AddRange(_BuildProperty(ObjectInstance, ObjectType, property_ids));
            _Register(_PropertyUpdaters);

            _TypeObjectNotifiables.AddRange(_BuildNotifier(ObjectInstance, ObjectType, property_ids));
            _Register(_TypeObjectNotifiables);
        }


        private IEnumerable<Tuple<int, PropertyInfo>> _GetProperties(Type soul_type, System.Collections.Generic.IReadOnlyDictionary<PropertyInfo, int> property_ids)
        {
            return from p in PropertyInfos
                   select new Tuple<int, PropertyInfo>(property_ids[p], p);
        }

        private IEnumerable<NotifierUpdater> _BuildNotifier(object soul, Type soul_type, System.Collections.Generic.IReadOnlyDictionary<PropertyInfo, int> propertyIds)
        {
            return from p in _GetProperties(soul_type, propertyIds)
                   let property = p.Item2
                   let id = p.Item1
                   where property.PropertyType.GetInterfaces().Any(t => t == typeof(ITypeObjectNotifiable))
                   let propertyValue = property.GetValue(soul)
                   select new NotifierUpdater(id, propertyValue as ITypeObjectNotifiable);
        }
        private IEnumerable<PropertyUpdater> _BuildProperty(object soul, Type soul_type, System.Collections.Generic.IReadOnlyDictionary<PropertyInfo, int> propertyIds)
        {
            foreach (Tuple<int, PropertyInfo> item in _GetProperties(soul_type, propertyIds))
            {
                PropertyInfo property = item.Item2;
                var id = item.Item1;
                if (property.PropertyType.GetInterfaces().Any(t => t == typeof(IDirtyable)))
                {
                    var propertyValue = property.GetValue(soul);
                    var dirtyable = propertyValue as IDirtyable;
                    yield return new PropertyUpdater(dirtyable, id);
                }
            }

        }



        private void _Register(List<NotifierUpdater> updaters)
        {
            foreach (NotifierUpdater updater in updaters)
            {
                updater.SupplyEvent += _SupplySoul;
                updater.UnsupplyEvent += _UnsupplySoul;
                updater.Initial();
            }
        }
        private void _Register(List<PropertyUpdater> property_updaters)
        {
            foreach (PropertyUpdater updater in property_updaters)
            {
                updater.ChnageEvent += _ChangeInvoke;
            }
        }

        private void _ChangeInvoke(IPropertyIdValue value, object instance)
        {
            PropertyChangedEvent.Invoke(Id, value);
        }

        internal void Release()
        {

            _Dispose();
        }

        private void _Unregister(List<PropertyUpdater> property_updaters)
        {
            foreach (PropertyUpdater updater in property_updaters)
            {

                updater.ChnageEvent -= _ChangeInvoke;
            }
        }

        private void _Unregister(List<NotifierUpdater> updaters)
        {
            foreach (NotifierUpdater updater in updaters)
            {
                updater.Final();
                updater.SupplyEvent -= _SupplySoul;
                updater.UnsupplyEvent -= _UnsupplySoul;
            }
        }

        private void _UnsupplySoul(int property_id, long soul_id)
        {
            UnsupplySoulEvent(Id, property_id, soul_id);
        }

        private ISoul _SupplySoul(int property_id, TypeObject type_object)
        {
            return SupplySoulEvent(Id, property_id, type_object);
        }

        internal void PropertyUpdateReset(int property)
        {


            foreach (PropertyUpdater updater in _PropertyUpdaters)
            {
                if (updater.PropertyId == property)
                {
                    updater.Reset();
                }
            }

        }



        internal void AddEvent(SoulProxyEventHandler handler)
        {
            lock (_EventHandlers)
                _EventHandlers.Add(handler);

        }

        internal void RemoveEvent(EventInfo eventInfo, long handler_id)
        {

            lock (_EventHandlers)
            {
                SoulProxyEventHandler eventHandler = _EventHandlers.FirstOrDefault(eh => eh.HandlerId == handler_id && eh.EventInfo == eventInfo);
                if (eventHandler == null)
                {
                    PinionCore.Utility.Log.Instance.WriteInfo($" RemoveEvent {handler_id} {eventInfo.Name} not found.");
                    return;
                }

                _EventHandlers.Remove(eventHandler);

                eventHandler.Release();
            }

        }

        bool ISoul.IsTypeObject(TypeObject obj)
        {
            return this.ObjectInstance == obj.Instance && this.ObjectType == obj.Type;
        }

        internal bool Is(Type declaring_type)
        {
            if (ObjectType.GetInterfaces().Any(i => i == declaring_type))
                return true;
            return declaring_type == ObjectType;
        }

        IEnumerable<Type> _GetInterfaces(Type object_type)
        {
            yield return object_type;
            foreach (Type item in object_type.GetInterfaces())
            {
                foreach (Type item2 in _GetInterfaces(item))
                {
                    yield return item2;
                }
            }
        }
    }

}

