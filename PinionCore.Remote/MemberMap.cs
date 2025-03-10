﻿using System;
using System.Collections.Generic;
using System.Reflection;
using PinionCore.Utility;

namespace PinionCore.Remote
{

    public class MemberMap : IEqualityComparer<Type>, IEqualityComparer<PropertyInfo>, IEqualityComparer<EventInfo>, IEqualityComparer<MethodInfo>, IEqualityComparer<int>
    {

        private readonly BilateralMap<int, MethodInfo> _Methods;
        private readonly Dictionary<int, EventInfo> _Events;
        private readonly BilateralMap<int, PropertyInfo> _Propertys;
        private readonly BilateralMap<int, Type> _Interfaces;
        private readonly Dictionary<Type, Func<IProvider>> _Providers;

        public readonly IReadOnlyBilateralMap<int, PropertyInfo> Propertys;


        public MemberMap(IEnumerable<MethodInfo> methods, System.Collections.Generic.Dictionary<int, EventInfo> events, IEnumerable<PropertyInfo> propertys, IEnumerable<System.Tuple<System.Type, System.Func<PinionCore.Remote.IProvider>>> interfaces)
        {
            //_Events = new Dictionary<int, EventInfo> { { 0, typeof(MemberMap).GetEvent("") } };
            _Providers = new Dictionary<Type, Func<IProvider>>();
            _Methods = new BilateralMap<int, MethodInfo>();
            _Events = events;
            _Propertys = new BilateralMap<int, PropertyInfo>();
            _Interfaces = new BilateralMap<int, Type>();


            var id = 0;
            foreach (MethodInfo method in methods)
            {
                _Methods.Add(++id, method);
            }



            id = 0;
            foreach (PropertyInfo propertyInfo in propertys)
            {

                _Propertys.Add(++id, propertyInfo);
            }

            id = 0;
            foreach (Tuple<Type, Func<IProvider>> @interface in interfaces)
            {
                _Interfaces.Add(++id, @interface.Item1);
                _Providers.Add(@interface.Item1, @interface.Item2);
            }


            Propertys = _Propertys;
        }

        public IProvider CreateProvider(Type type)
        {
            Func<IProvider> provider;
            if (_Providers.TryGetValue(type, out provider))
            {
                return provider();
            }
            throw new SystemException($"no provider in type {type}");

        }
        public MethodInfo GetMethod(int id)
        {
            MethodInfo method;
            _Methods.TryGetItem2(id, out method);
            return method;
        }

        public int GetMethod(MethodInfo method_info)
        {
            var id = 0;
            _Methods.TryGetItem1(method_info, out id);
            return id;
        }

        public EventInfo GetEvent(int id)
        {
            EventInfo info;
            _Events.TryGetValue(id, out info);
            return info;
        }

        public int GetProperty(PropertyInfo info)
        {
            var id = 0;
            _Propertys.TryGetItem1(info, out id);
            return id;
        }

        public PropertyInfo GetProperty(int id)
        {
            PropertyInfo info;
            _Propertys.TryGetItem2(id, out info);
            return info;
        }

        private string _GetMethod(Type type, string method)
        {
            return string.Format("{0}.{1}", type.FullName, method);
        }

        bool IEqualityComparer<MethodInfo>.Equals(MethodInfo x, MethodInfo y)
        {
            return _GetMethod(x.DeclaringType, x.Name) == _GetMethod(y.DeclaringType, y.Name);
        }

        int IEqualityComparer<MethodInfo>.GetHashCode(MethodInfo obj)
        {
            return obj.GetHashCode();
        }

        bool IEqualityComparer<int>.Equals(int x, int y)
        {
            return x == y;
        }

        int IEqualityComparer<int>.GetHashCode(int obj)
        {
            return obj.GetHashCode();
        }


        private string _GetEvent(Type type, string name)
        {
            PinionCore.Utility.Log.Instance.WriteInfoImmediate($"get event {type.FullName}_{name}");
            return string.Format("{0}_{1}", type.FullName, name);
        }


        bool IEqualityComparer<EventInfo>.Equals(EventInfo x, EventInfo y)
        {
            return _GetEvent(x.DeclaringType, x.Name) == _GetEvent(y.DeclaringType, y.Name);
        }

        int IEqualityComparer<EventInfo>.GetHashCode(EventInfo obj)
        {
            return obj.GetHashCode();
        }


        private string _GetProperty(Type type, string name)
        {
            return string.Format("{0}+{1}", type.FullName, name);
        }


        bool IEqualityComparer<PropertyInfo>.Equals(PropertyInfo x, PropertyInfo y)
        {
            return _GetProperty(x.DeclaringType, x.Name) == _GetProperty(y.DeclaringType, y.Name);
        }

        int IEqualityComparer<PropertyInfo>.GetHashCode(PropertyInfo obj)
        {
            return obj.GetHashCode();
        }

        public Type GetInterface(int type_id)
        {
            Type type;
            _Interfaces.TryGetItem2(type_id, out type);
            return type;
        }

        public int GetInterface(Type type)
        {
            int id;
            _Interfaces.TryGetItem1(type, out id);
            return id;
        }

        bool IEqualityComparer<Type>.Equals(Type x, Type y)
        {
            return x.FullName == y.FullName;
        }

        int IEqualityComparer<Type>.GetHashCode(Type obj)
        {
            return obj.GetHashCode();
        }
    }
}
