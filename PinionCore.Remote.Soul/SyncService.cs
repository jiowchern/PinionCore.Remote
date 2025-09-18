using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PinionCore.Remote.Soul
{
    public class SyncService : System.IDisposable
    {
        readonly IEntry _Entry;
        readonly UserProvider _UserProvider;
        readonly IDisposable _UserProviderDisposable;

        private readonly IProtocol _Protocol;
        private readonly ISerializable _Serializable;
        private readonly IInternalSerializable _InternalSerializable;
        private readonly PinionCore.Memorys.IPool _Pool;

        private readonly ConcurrentDictionary<Network.IStreamable, User> _Users;

        private struct Pending
        {
            public bool IsJoin;
            public Network.IStreamable Stream;
            public PinionCore.Network.PackageReader Reader;
            public PinionCore.Network.PackageSender Sender;
        }

        private readonly ConcurrentQueue<Pending> _Pending;
        

        public SyncService(IEntry entry, IProtocol protocol, ISerializable serializable, IInternalSerializable internal_serializable, PinionCore.Memorys.IPool pool, UserProvider user_provider)
        {
            _Entry = entry;
            _Protocol = protocol;
            _Serializable = serializable;
            _InternalSerializable = internal_serializable;
            _Pool = pool;

            _UserProvider = user_provider;
            _UserProviderDisposable = user_provider;

            _Users = new ConcurrentDictionary<Network.IStreamable, User>();
            _Pending = new ConcurrentQueue<Pending>();
            

            _UserProvider.JoinEvent += _OnJoin;
            _UserProvider.LeaveEvent += _OnLeave;
        }

        public event System.Action UsersStateChangedEvent;

        private void _OnJoin(Network.IStreamable stream, PinionCore.Network.PackageReader reader, PinionCore.Network.PackageSender sender)
        {
            _Pending.Enqueue(new Pending { IsJoin = true, Stream = stream, Reader = reader, Sender = sender });
         
        }

        private void _OnLeave(Network.IStreamable stream)
        {
            _Pending.Enqueue(new Pending { IsJoin = false, Stream = stream, Reader = null, Sender = null });
         
        }

        public void Update()
        {
            _Entry.Update();

            // Drain and process all pending join/leave on this thread
            while (_Pending.TryDequeue(out Pending ev))
            {
                if (ev.IsJoin)
                {
                    var user = new User(ev.Reader, ev.Sender, _Protocol, _Serializable, _InternalSerializable, _Pool);
                    var capturedStream = ev.Stream;
                    var capturedSender = ev.Sender as IDisposable;
                    user.ErrorEvent += () =>
                    {
                        capturedSender?.Dispose();
                        _OnLeave(capturedStream);
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
                        var dispose = ev.Sender as IDisposable;
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
                UsersStateChangedEvent?.Invoke();
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
            _UserProvider.JoinEvent -= _OnJoin;
            _UserProvider.LeaveEvent -= _OnLeave;
            _UserProviderDisposable.Dispose();
            
        }
    }
}

