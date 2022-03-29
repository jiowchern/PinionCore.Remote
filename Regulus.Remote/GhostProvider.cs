﻿using Regulus.Remote.Extensions;
using Regulus.Remote.Packages;
using Regulus.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Regulus.Remote
{
    delegate System.Action<object> GetObjectAccesserMethod(IObjectAccessible accessible);
    public class GhostProvider
    {
        private readonly AutoRelease _AutoRelease;

        private readonly Dictionary<Type, IProvider> _Providers;

        private readonly Dictionary<long, GhostResponseHandler> _Ghosts;

        private readonly ReturnValueQueue _ReturnValueQueue;

        private readonly object _Sync = new object();

        private readonly TimeCounter _PingTimeCounter = new TimeCounter();

        private Timer _PingTimer;

        private readonly IGhostRequest _Requester;

        private readonly InterfaceProvider _InterfaceProvider;
        private readonly ISerializable _Serializer;        
       
        private readonly IProtocol _Protocol;

        bool _Active;
        public bool Active => _Active;

        public long Ping { get; private set; }

        readonly IInternalSerializable _InternalSerializer;


        public GhostProvider(IProtocol protocol, ISerializable serializable, IInternalSerializable internal_serializable, IGhostRequest req)
        {
            _InternalSerializer = internal_serializable;
            _Active = false;
            _Requester = req;

            _ReturnValueQueue = new ReturnValueQueue();
            _Protocol = protocol;
            _InterfaceProvider = _Protocol.GetInterfaceProvider();
            _Serializer = serializable;
            _Providers = new Dictionary<Type, IProvider>();
            _Ghosts = new Dictionary<long, GhostResponseHandler>();
            _AutoRelease = new AutoRelease(_Requester, _InternalSerializer);
        }
        public void Start()
        {
            _Requester.ResponseEvent += OnResponse;
            _StartPing();            
        }
        public void Stop()
        {
            _Requester.ResponseEvent -= OnResponse;
            
            lock (_Providers)
            {
                foreach (KeyValuePair<Type, IProvider> providerPair in _Providers)
                {
                    providerPair.Value.ClearGhosts();
                }
            }
            lock(_Ghosts)
                _Ghosts.Clear();

            _EndPing();
        }

        public void OnResponse(ServerToClientOpCode id, byte[] args)
        {
            _OnResponse(id, args);
            _AutoRelease.Update();
        }

        protected void _OnResponse(ServerToClientOpCode id, byte[] args)
        {
            if (id == ServerToClientOpCode.Ping)
            {                
                Ping = _PingTimeCounter.Ticks;
                _StartPing();
            }
            else if (id == ServerToClientOpCode.SetProperty)
            {

                Regulus.Remote.Packages.PackageSetProperty data = (Regulus.Remote.Packages.PackageSetProperty)_InternalSerializer.Deserialize(args)   ;
                _UpdateSetProperty(data.EntityId, data.Property, data.Value);
            }

            else if (id == ServerToClientOpCode.InvokeEvent)
            {
                Regulus.Remote.Packages.PackageInvokeEvent data = (Regulus.Remote.Packages.PackageInvokeEvent)_InternalSerializer.Deserialize(args)  ;
                _InvokeEvent(data.EntityId, data.Event, data.HandlerId, data.EventParams);
            }
            else if (id == ServerToClientOpCode.ErrorMethod)
            {
                Regulus.Remote.Packages.PackageErrorMethod data = (Regulus.Remote.Packages.PackageErrorMethod)_InternalSerializer.Deserialize(args)  ;

                _ErrorReturnValue(data.ReturnTarget, data.Method, data.Message);
            }
            else if (id == ServerToClientOpCode.ReturnValue)
            {
                Regulus.Remote.Packages.PackageReturnValue data = (Regulus.Remote.Packages.PackageReturnValue)_InternalSerializer.Deserialize(args)  ;
                _SetReturnValue(data.ReturnTarget, data.ReturnValue);
            }
            else if (id == ServerToClientOpCode.LoadSoulCompile)
            {

                Regulus.Remote.Packages.PackageLoadSoulCompile data = (Regulus.Remote.Packages.PackageLoadSoulCompile)_InternalSerializer.Deserialize(args)  ;
                _LoadSoulCompile(data.TypeId, data.EntityId, data.ReturnId);
                
            }            
            else if (id == ServerToClientOpCode.LoadSoul)
            {
                Regulus.Remote.Packages.PackageLoadSoul data = (Regulus.Remote.Packages.PackageLoadSoul)_InternalSerializer.Deserialize(args)  ;
                _LoadSoul(data.TypeId, data.EntityId, data.ReturnType);                
            }
            else if (id == ServerToClientOpCode.UnloadSoul)
            {
                Regulus.Remote.Packages.PackageUnloadSoul data = (Regulus.Remote.Packages.PackageUnloadSoul)_InternalSerializer.Deserialize(args)  ;

                _UnloadSoul(data.EntityId);
            }
            else if (id == ServerToClientOpCode.AddPropertySoul)
            {
                Regulus.Remote.Packages.PackagePropertySoul data = (Regulus.Remote.Packages.PackagePropertySoul)_InternalSerializer.Deserialize(args)  ;

                _AddPropertySoul(data);
            }
            else if (id == ServerToClientOpCode.RemovePropertySoul)
            {
                Regulus.Remote.Packages.PackagePropertySoul data = (Regulus.Remote.Packages.PackagePropertySoul)_InternalSerializer.Deserialize(args)  ;

                _RemovePropertySoul(data);
            }
            else if (id == ServerToClientOpCode.ProtocolSubmit)
            {
                Regulus.Remote.Packages.PackageProtocolSubmit data = (Regulus.Remote.Packages.PackageProtocolSubmit)_InternalSerializer.Deserialize(args)  ;

                _ProtocolSubmit(data);
            }
        }
        private void _ProtocolSubmit(Regulus.Remote.Packages.PackageProtocolSubmit data)
        {
            _Active = _Comparison(_Protocol.VerificationCode, data.VerificationCode);

        }

        private bool _Comparison(byte[] code1, byte[] code2)
        {
            return new Regulus.Utility.Comparer<byte>(code1, code2, (arg1, arg2) => arg1 == arg2).Same;
        }

        private void _ErrorReturnValue(long return_target, string method, string message)
        {
            _ReturnValueQueue.PopReturnValue(return_target);

            if (ErrorMethodEvent != null)
            {
                ErrorMethodEvent(method, message);
            }
        }

        private void _SetReturnValue(long returnTarget, byte[] returnValue)
        {
            IValue value = _ReturnValueQueue.PopReturnValue(returnTarget);
            if (value != null)
            {
                object returnInstance = _Serializer.Deserialize(value.GetObjectType() , returnValue);
                value.SetValue(returnInstance);
            }
        }

        private void _ReturnValue(long return_id, IGhost ghost)
        {
            IValue value = _ReturnValueQueue.PopReturnValue(return_id);
            if (value != null)
            {
                value.SetValue(ghost);
            }
        }

        private void _LoadSoulCompile(int type_id, long entity_id, long return_id )
        {
            
            MemberMap map = _Protocol.GetMemberMap();
            
            Type type = map.GetInterface(type_id);
            
            IProvider provider = _QueryProvider(type);

            IGhost ghost = provider.Ready(entity_id);            
            if (return_id != 0)
                _ReturnValue(return_id, ghost);
        }

        

        private void _LoadSoul(int type_id, long id, bool return_type)
        {
            MemberMap map = _Protocol.GetMemberMap();
            Type type = map.GetInterface(type_id);
            IProvider provider = _QueryProvider(type);
            IGhost ghost = _BuildGhost(type, id, return_type);

            var weak = new WeakReference<IGhost>(ghost);
            ghost.CallMethodEvent += new GhostMethodHandler(weak, _ReturnValueQueue, _Protocol, _Serializer, _InternalSerializer, _Requester).Run;
            ghost.AddEventEvent += new GhostEventMoveHandler(weak, _Protocol, _InternalSerializer, _Requester).Add;
            ghost.RemoveEventEvent += new GhostEventMoveHandler(weak, _Protocol, _InternalSerializer, _Requester).Remove;            

            provider.Add(ghost);
            lock (_Ghosts)
                _Ghosts.Add(ghost.GetID(), new GhostResponseHandler(weak, _Protocol.GetMemberMap(), _Serializer));
            if (ghost.IsReturnType())
            {
                _RegisterRelease(ghost);
            }
        }

        private void _RegisterRelease(IGhost ghost)
        {
            _AutoRelease.Register(ghost);
        }

        private void _UnloadSoul(long id)
        {
            lock(_Providers)
            {
                foreach (var provider in _Providers.Values)
                {
                    provider.Remove(id);
                }
            }
            lock (_Ghosts)
                _Ghosts.Remove(id);
        }

        private IProvider _QueryProvider(Type type)
        {
            IProvider provider = null;
            lock (_Providers)
            {
                if (_Providers.TryGetValue(type, out provider) == false)
                {
                    provider = _BuildProvider(type);
                    _Providers.Add(type, provider);
                }
            }

            return provider;
        }

        private IProvider _BuildProvider(Type type)
        {
            MemberMap map = _Protocol.GetMemberMap();
            return map.CreateProvider(type);
        }
        public INotifier<T> QueryProvider<T>()
        {
            return _QueryProvider(typeof(T)) as INotifier<T>;
        }
        private void _RemovePropertySoul(Regulus.Remote.Packages.PackagePropertySoul data)
        {
            _PropertySoulAccesser(data, (a) => a.Remove);            
        }
        private void _AddPropertySoul(Regulus.Remote.Packages.PackagePropertySoul data)
        {
            _PropertySoulAccesser(data, (a) => a.Add);
        }

        internal void _PropertySoulAccesser(PackagePropertySoul data, System.Linq.Expressions.Expression<GetObjectAccesserMethod> oper)
        {

            var owner_handler = _FindGhost(data.OwnerId);
            if (owner_handler == null)
                return;

            var entity = _FindGhost(data.EntityId);
            if (entity == null)
                return;

            var entity_ghost = entity.FindGhost();

            oper.Execute().Invoke(owner_handler.GetAccesser(data.PropertyId) , new object[] { entity_ghost.GetInstance() });
        }

        

        

        private void _UpdateSetProperty(long entity_id, int property, byte[] payload)
        {
            var ghost = _FindGhost(entity_id);
            if (ghost == null)
                return;
            ghost.UpdateSetProperty(property, payload);

            Regulus.Remote.Packages.PackageSetPropertyDone pkg = new Regulus.Remote.Packages.PackageSetPropertyDone();
            pkg.EntityId = entity_id;
            pkg.Property = property;
            
            _Requester.Request(ClientToServerOpCode.UpdateProperty, _InternalSerializer.Serialize(pkg));
        }

        private void _InvokeEvent(long ghost_id, int event_id, long handler_id, byte[][] event_params)
        {
            var ghost = _FindGhost(ghost_id);
            if (ghost == null)
            {
                return;
            }

            ghost.InvokeEvent(event_id, handler_id, event_params);

        }       

        private GhostResponseHandler _FindGhost(long ghost_id)
        {
            GhostResponseHandler handler = null;
            lock (_Ghosts)
            {
                if(_Ghosts.ContainsKey(ghost_id))
                {
                    handler = _Ghosts[ghost_id];
                    
                    if (handler.IsValid())
                    {
                        return handler;
                    }                        
                    else
                    {
                        _Ghosts.Remove(ghost_id);
                    }
                }

            }
            
            return handler;
        }

        protected void _StartPing()
        {
            _EndPing();
            lock (_Sync)
            {
                _PingTimer = new Timer(1000);
                _PingTimer.Enabled = true;
                _PingTimer.AutoReset = true;
                _PingTimer.Elapsed += _PingTimerElapsed;
                _PingTimer.Start();
            }
        }

        private void _PingTimerElapsed(object sender, ElapsedEventArgs e)
        {
            lock (_Sync)
            {
                if (_PingTimer != null)
                {
                    _PingTimeCounter.Reset();
                    _Requester.Request(ClientToServerOpCode.Ping, new byte[0]);
                }
            }

            _EndPing();
        }

        protected void _EndPing()
        {
            lock (_Sync)
            {
                if (_PingTimer != null)
                {
                    _PingTimer.Stop();
                    _PingTimer = null;
                }
            }
        }

        private IGhost _BuildGhost(Type ghost_base_type, long id, bool return_type)
        {
            Type ghostType = _QueryGhostType(ghost_base_type);

            ConstructorInfo constructor = ghostType.GetConstructor(new[] { typeof(long), typeof(bool) });
            /*if (constructor == null)
            {
                List<string> constructorInfos = new List<string>();

                foreach (ConstructorInfo constructorInfo in ghostType.GetConstructors())
                {
                    constructorInfos.Add("(" + constructorInfo.GetParameters() + ")");

                }
                throw new Exception(string.Format("{0} Not found constructor.\n{1}", ghostType.FullName, string.Join("\n", constructorInfos.ToArray())));
            }*/
            object o = constructor.Invoke(new object[] { id, return_type });

            return (IGhost)o;
        }



        private Type _QueryGhostType(Type ghostBaseType)
        {
            return _InterfaceProvider.Find(ghostBaseType);
        }

        public event Action<string, string> ErrorMethodEvent;
    }
}
