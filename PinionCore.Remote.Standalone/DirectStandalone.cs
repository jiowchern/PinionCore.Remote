using System;
using System.Collections.Generic;
using PinionCore.Network;

namespace PinionCore.Remote.Standalone
{
    /// <summary>
    /// 直通單機模式：同時扮演客戶端 Ghost.IAgent 與伺服器端 ISessionBinder，
    /// 將 Bind 進來的 Soul 實例「不經序列化」直接供給到 QueryNotifier&lt;T&gt;。
    ///
    /// 與 ListeningEndpoint（保留完整序列化管線的單機模式）不同，
    /// 本類別完全繞過 IProtocol、序列化與 Ghost 生成程式碼——
    /// 客戶端經 Supply 取得的物件就是伺服器端實例本身（共用參考），
    /// 之後的方法呼叫、事件、Property、Spirit&lt;T&gt; 都是直接的 .NET 呼叫。
    ///
    /// 為與網路模式時序一致，Bind/Unbind 造成的 Supply/Unsupply 不會立即觸發，
    /// 而是排入佇列、於 HandleMessages() 時依序發生；
    /// HandlePackets() 則負責驅動 IEntry.Update()（對應網路模式由服務執行緒驅動），
    /// 使用端不需另外驅動 IEntry.Update()。
    ///
    /// 注意：本模式不驗證可序列化性——在此能運作的 Spirit 介面不代表可遠端化，
    /// 上線前仍應以 ListeningEndpoint 或 TCP 模式進行整合測試。
    /// </summary>
    public class DirectStandalone : PinionCore.Remote.Ghost.IAgent, ISessionBinder, IDisposable
    {
        private readonly IEntry _Entry;
        private readonly Dictionary<Type, object> _Depots;
        private readonly System.Collections.Concurrent.ConcurrentQueue<Action> _Operations;
        private readonly List<SoulHandle> _Souls;
        private long _IdSn;
        private bool _Launched;
        private bool _Disposed;

        public DirectStandalone(IEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            _Entry = entry;
            _Depots = new Dictionary<Type, object>();
            _Operations = new System.Collections.Concurrent.ConcurrentQueue<Action>();
            _Souls = new List<SoulHandle>();
        }

        /// <summary>
        /// 開啟會話：觸發 IEntry.OnSessionOpened 並傳入本物件作為 ISessionBinder。
        /// 之後 Entry 端的 Bind 會於 HandleMessages() 時供給到 QueryNotifier。
        /// </summary>
        public void Launch()
        {
            if (_Disposed)
                throw new ObjectDisposedException(nameof(DirectStandalone));
            if (_Launched)
                return;

            _Launched = true;
            ISessionBinder binder = this;
            _Entry.OnSessionOpened(binder);
        }

        /// <summary>
        /// 關閉會話：觸發 IEntry.OnSessionClosed，並比照網路模式 Disable 的同步語意，
        /// 立即處理佇列中的操作並撤銷（Unsupply）所有仍綁定的 Soul。
        /// </summary>
        public void Shutdown()
        {
            if (!_Launched)
                return;

            _Launched = false;
            ISessionBinder binder = this;
            _Entry.OnSessionClosed(binder);

            while (_Operations.TryDequeue(out Action operation))
            {
                operation();
            }

            SoulHandle[] remains;
            lock (_Souls)
            {
                remains = _Souls.ToArray();
                _Souls.Clear();
            }
            foreach (SoulHandle soul in remains)
            {
                soul.Remove();
            }
        }

        void IDisposable.Dispose()
        {
            if (_Disposed)
                return;

            Shutdown();
            _Disposed = true;
        }

        INotifier<T> INotifierQueryable.QueryNotifier<T>()
        {
            return _QueryDepot<T>().Notifier;
        }

        ISoul ISessionBinder.Bind<TSoul>(TSoul soul)
        {
            return _Bind(soul, true);
        }

        // 比照遠端模式：Return 的物件不進 QueryNotifier（僅作為方法回傳流程的一部分），
        // 直通模式下回傳值本來就直接共享，這裡只需要保留可 Unbind 的追蹤。
        ISoul ISessionBinder.Return<TSoul>(TSoul soul)
        {
            return _Bind(soul, false);
        }

        void ISessionBinder.Unbind(ISoul soul)
        {
            SoulHandle handle = soul as SoulHandle;
            bool removed = false;
            if (handle != null)
            {
                lock (_Souls)
                {
                    removed = _Souls.Remove(handle);
                }
            }

            if (!removed)
                throw new Exception($"Can't find the soul {soul.Id} to delete.");

            _Operations.Enqueue(handle.Remove);
        }

        float PinionCore.Remote.Ghost.IAgent.Ping
        {
            get { return 0f; }
        }

        // 直通模式無序列化與版本協商，以下事件永不觸發。
        event Action<byte[], byte[]> PinionCore.Remote.Ghost.IAgent.VersionCodeErrorEvent
        {
            add { }
            remove { }
        }

        event Action<string, string> PinionCore.Remote.Ghost.IAgent.ErrorMethodEvent
        {
            add { }
            remove { }
        }

        event Action<Exception> PinionCore.Remote.Ghost.IAgent.ExceptionEvent
        {
            add { }
            remove { }
        }

        // 對應網路模式由服務執行緒驅動 IEntry.Update：直通模式由客戶端主迴圈代驅。
        void PinionCore.Remote.Ghost.IAgent.HandlePackets()
        {
            if (_Launched)
                _Entry.Update();
        }

        void PinionCore.Remote.Ghost.IAgent.HandleMessages()
        {
            while (_Operations.TryDequeue(out Action operation))
            {
                operation();
            }
        }

        // streamable 在直通模式沒有作用（傳 null 即可），僅為滿足 IAgent 介面而映射到 Launch/Shutdown。
        void PinionCore.Remote.Ghost.IAgent.Enable(IStreamable streamable)
        {
            Launch();
        }

        void PinionCore.Remote.Ghost.IAgent.Disable()
        {
            Shutdown();
        }

        private ISoul _Bind<TSoul>(TSoul soul, bool visible)
        {
            if (soul == null)
                throw new ArgumentNullException(nameof(soul));
            if (_Disposed)
                throw new ObjectDisposedException(nameof(DirectStandalone));

            var id = System.Threading.Interlocked.Increment(ref _IdSn);
            Action remove = () => { };
            if (visible)
            {
                Depot<TSoul> depot = _QueryDepot<TSoul>();
                _Operations.Enqueue(() => depot.Items.Add(soul));
                remove = () => depot.Items.Remove(soul);
            }

            var handle = new SoulHandle(id, typeof(TSoul), soul, remove);
            lock (_Souls)
            {
                _Souls.Add(handle);
            }
            return handle;
        }

        private Depot<T> _QueryDepot<T>()
        {
            lock (_Depots)
            {
                if (_Depots.TryGetValue(typeof(T), out var value))
                {
                    // 以 typeof(T) 為 key 寫入，型別必然相符
                    return value as Depot<T>;
                }

                var depot = new Depot<T>();
                _Depots.Add(typeof(T), depot);
                return depot;
            }
        }

        private sealed class SoulHandle : ISoul
        {
            private readonly Type _Type;
            public readonly Action Remove;

            public SoulHandle(long id, Type type, object instance, Action remove)
            {
                Id = id;
                _Type = type;
                Instance = instance;
                Remove = remove;
            }

            public object Instance { get; }

            public long Id { get; }

            bool ISoul.IsTypeObject(TypeObject obj)
            {
                return obj.Type == _Type && ReferenceEquals(obj.Instance, Instance);
            }
        }
    }
}
