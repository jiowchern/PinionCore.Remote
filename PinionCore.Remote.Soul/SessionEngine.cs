using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using PinionCore.Extensions;
using PinionCore.Network;

namespace PinionCore.Remote.Soul
{
    public class SessionEngine : System.IDisposable , IService
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

        // host 的執行緒政策,交給每個 User 與 PackageSender
        private readonly Network.IThreading _Threading;

        // 任一 session 有封包抵達時觸發(來自封包 I/O 執行緒),供更新迴圈立即喚醒
        public event Action PacketArrivedEvent;

        public SessionEngine(IEntry entry, IProtocol protocol, ISerializable serializable, IInternalSerializable internal_serializable, PinionCore.Memorys.IPool pool)
            : this(entry, protocol, serializable, internal_serializable, pool, new Network.InlineThreading())
        {
        }

        public SessionEngine(IEntry entry, IProtocol protocol, ISerializable serializable, IInternalSerializable internal_serializable, PinionCore.Memorys.IPool pool, Network.IThreading threading)
        {
            _Threading = threading;
            _Entry = entry;
            _Protocol = protocol;
            PinionCore.Utility.Log.Instance.WriteInfo("SyncService Protocol: " + protocol.VersionCode.ToMd5String());
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
                    var sender = new PinionCore.Network.PackageSender(ev.Stream, _Pool, _Threading);
                    var user = new User(reader, sender, _Protocol, _Serializable, _InternalSerializable, _Pool, _Threading);
                    user.DataArrivedEvent += _NotifyPacketArrived;
                    var capturedStream = ev.Stream;
                    user.ErrorEvent += () =>
                    {
                        IDisposable userDispose = user;
                        userDispose.Dispose();

                        IDisposable streamDispose = capturedStream;
                        streamDispose.Dispose();

                        Leave(capturedStream);
                    };
                    user.Launch();
                    if (_Users.TryAdd(ev.Stream, user))
                    {
                        _Entry.OnSessionOpened(user.Binder);
                    }
                    else
                    {
                        // already exists; clean up the extra instance
                        user.DataArrivedEvent -= _NotifyPacketArrived;
                        IDisposable userDispose = user;
                        userDispose.Dispose();

                        IDisposable streamDispose = ev.Stream;
                        streamDispose.Dispose();
                    }
                }
                else
                {
                    if (_Users.TryRemove(ev.Stream, out User user))
                    {
                        user.DataArrivedEvent -= _NotifyPacketArrived;
                        _Entry.OnSessionClosed(user.Binder);

                        IDisposable userDispose = user;
                        userDispose.Dispose();

                        IDisposable streamDispose = ev.Stream;
                        streamDispose.Dispose();
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

        private void _NotifyPacketArrived()
        {
            PacketArrivedEvent?.Invoke();
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

