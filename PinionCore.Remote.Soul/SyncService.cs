using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using PinionCore.Network;

namespace PinionCore.Remote.Soul
{
    public class SyncService : System.IDisposable , IService
    {
        readonly IEntry _Entry;
        private readonly IProtocol _Protocol;
        private readonly ISerializable _Serializable;
        private readonly IInternalSerializable _InternalSerializable;
        private readonly PinionCore.Memorys.IPool _Pool;

        private readonly ConcurrentDictionary<Network.IStreamable, User> _Users;

        private struct Pending
        {
            public bool IsJoin;
            public Network.IStreamable Stream;            
        }

        private readonly ConcurrentQueue<Pending> _Pending;
        

        public SyncService(IEntry entry, IProtocol protocol, ISerializable serializable, IInternalSerializable internal_serializable, PinionCore.Memorys.IPool pool)
        {
            _Entry = entry;
            _Protocol = protocol;
            _Serializable = serializable;
            _InternalSerializable = internal_serializable;
            _Pool = pool;

            _Users = new ConcurrentDictionary<Network.IStreamable, User>();
            _Pending = new ConcurrentQueue<Pending>();
            
        }

        

        void Join(Network.IStreamable stream)
        {
            _Pending.Enqueue(new Pending { IsJoin = true, Stream = stream});         
        }

        void Leave(Network.IStreamable stream)
        {
            _Pending.Enqueue(new Pending { IsJoin = false, Stream = stream});         
        }

        public void Update()
        {
            _Entry.Update();

            // Drain and process all pending join/leave on this thread
            while (_Pending.TryDequeue(out Pending ev))
            {
                if (ev.IsJoin)
                {
                    var reader = new PinionCore.Network.PackageReader(ev.Stream, _Pool);
                    var sender = new PinionCore.Network.PackageSender(ev.Stream, _Pool);
                    var user = new User(reader, sender, _Protocol, _Serializable, _InternalSerializable, _Pool);
                    var capturedStream = ev.Stream;
                    var capturedSender = sender as IDisposable;
                    user.ErrorEvent += () =>
                    {
                        capturedSender?.Dispose();
                        Leave(capturedStream);
                    };
                    user.Launch();
                    if (_Users.TryAdd(ev.Stream, user))
                    {
                        _Entry.RegisterClientBinder(user.Binder);
                    }
                    else
                    {
                        // already exists; clean up the extra instance
                        user.Shutdown();
                        var dispose = sender as IDisposable;
                        dispose.Dispose();
                    }
                }
                else
                {
                    if (_Users.TryRemove(ev.Stream, out User user))
                    {
                        user.Shutdown();
                        _Entry.UnregisterClientBinder(user.Binder);
                    }
                }                
            }
            

            foreach (Advanceable user in _Users.Values)
            {
                try
                {
                    user.Advance();
                }
                catch (Exception e)
                {
                    PinionCore.Utility.Log.Instance.WriteInfo($"Advance Error: {e.ToString()}");
                }
            }

        }

        void IDisposable.Dispose()
        {            
            
        }

        void IService.Join(IStreamable user)
        {
            Join(user);
        }

        void IService.Leave(IStreamable user)
        {
            Leave(user);
        }
    }
}

