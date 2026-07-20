using System;
using NUnit.Framework;

namespace PinionCore.Remote.Standalone.Test
{
    // 直通模式測試用 Spirit：不需要 Protocol 生成，介面存在即可。
    public interface IDirectChat
    {
        PinionCore.Remote.Value<int> Add(int a, int b);
        event Action<string> MessageEvent;
    }

    public class DirectChat : IDirectChat
    {
        public event Action<string> MessageEvent;

        PinionCore.Remote.Value<int> IDirectChat.Add(int a, int b)
        {
            return a + b;
        }

        public void Broadcast(string message)
        {
            MessageEvent?.Invoke(message);
        }
    }

    public class DirectEntry : IEntry
    {
        public Action<ISessionBinder> OpenedHandler = _ => { };
        public Action<ISessionBinder> ClosedHandler = _ => { };
        public int UpdateCount;

        void ISessionObserver.OnSessionOpened(ISessionBinder binder)
        {
            OpenedHandler(binder);
        }

        void ISessionObserver.OnSessionClosed(ISessionBinder binder)
        {
            ClosedHandler(binder);
        }

        void IEntry.Update()
        {
            UpdateCount++;
        }
    }

    public class DirectStandaloneTests
    {
        [Test, Timeout(10000)]
        public void BindSuppliesSameInstanceOnHandleMessagesTest()
        {
            var chat = new DirectChat();
            var entry = new DirectEntry();
            entry.OpenedHandler = binder => binder.Bind<IDirectChat>(chat);

            var direct = new DirectStandalone(entry);
            PinionCore.Remote.Ghost.IAgent agent = direct;

            IDirectChat supplied = null;
            agent.QueryNotifier<IDirectChat>().Supply += c => supplied = c;

            direct.Launch();
            // 比照網路模式時序：HandleMessages 前不觸發 Supply
            Assert.IsNull(supplied);

            agent.HandleMessages();
            Assert.AreSame(chat, supplied);

            IDisposable disposable = direct;
            disposable.Dispose();
        }

        [Test, Timeout(10000)]
        public void LateSubscribeReplayTest()
        {
            var chat = new DirectChat();
            var entry = new DirectEntry();
            entry.OpenedHandler = binder => binder.Bind<IDirectChat>(chat);

            var direct = new DirectStandalone(entry);
            PinionCore.Remote.Ghost.IAgent agent = direct;

            direct.Launch();
            agent.HandleMessages();

            // 晚訂閱也能收到已供給的實例（Depot 補發語意）
            IDirectChat supplied = null;
            agent.QueryNotifier<IDirectChat>().Supply += c => supplied = c;
            Assert.AreSame(chat, supplied);

            IDisposable disposable = direct;
            disposable.Dispose();
        }

        [Test, Timeout(10000)]
        public void UnbindRaisesUnsupplyOnHandleMessagesTest()
        {
            var chat = new DirectChat();
            var entry = new DirectEntry();
            ISoul soul = null;
            ISessionBinder sessionBinder = null;
            entry.OpenedHandler = binder =>
            {
                sessionBinder = binder;
                soul = binder.Bind<IDirectChat>(chat);
            };

            var direct = new DirectStandalone(entry);
            PinionCore.Remote.Ghost.IAgent agent = direct;

            IDirectChat unsupplied = null;
            agent.QueryNotifier<IDirectChat>().Unsupply += c => unsupplied = c;

            direct.Launch();
            agent.HandleMessages();

            sessionBinder.Unbind(soul);
            Assert.IsNull(unsupplied);

            agent.HandleMessages();
            Assert.AreSame(chat, unsupplied);

            IDisposable disposable = direct;
            disposable.Dispose();
        }

        [Test, Timeout(10000)]
        public void ShutdownUnsuppliesAllAndClosesSessionTest()
        {
            var chat = new DirectChat();
            var entry = new DirectEntry();
            entry.OpenedHandler = binder => binder.Bind<IDirectChat>(chat);
            var closed = false;
            entry.ClosedHandler = _ => closed = true;

            var direct = new DirectStandalone(entry);
            PinionCore.Remote.Ghost.IAgent agent = direct;

            IDirectChat unsupplied = null;
            agent.QueryNotifier<IDirectChat>().Unsupply += c => unsupplied = c;

            direct.Launch();
            agent.HandleMessages();

            // 比照網路模式 Disable 的同步語意：Shutdown 立即撤銷，不需再 pump
            direct.Shutdown();
            Assert.IsTrue(closed);
            Assert.AreSame(chat, unsupplied);

            IDisposable disposable = direct;
            disposable.Dispose();
        }

        [Test, Timeout(10000)]
        public void ReturnDoesNotSupplyNotifierTest()
        {
            var chat = new DirectChat();
            var entry = new DirectEntry();
            ISoul soul = null;
            ISessionBinder sessionBinder = null;
            entry.OpenedHandler = binder =>
            {
                sessionBinder = binder;
                soul = binder.Return<IDirectChat>(chat);
            };

            var direct = new DirectStandalone(entry);
            PinionCore.Remote.Ghost.IAgent agent = direct;

            IDirectChat supplied = null;
            agent.QueryNotifier<IDirectChat>().Supply += c => supplied = c;

            direct.Launch();
            agent.HandleMessages();

            // 比照遠端模式：Return 的物件不進 QueryNotifier
            Assert.IsNull(supplied);
            Assert.DoesNotThrow(() => sessionBinder.Unbind(soul));

            IDisposable disposable = direct;
            disposable.Dispose();
        }

        [Test, Timeout(10000)]
        public void HandlePacketsDrivesEntryUpdateTest()
        {
            var entry = new DirectEntry();
            var direct = new DirectStandalone(entry);
            PinionCore.Remote.Ghost.IAgent agent = direct;

            agent.HandlePackets();
            Assert.AreEqual(0, entry.UpdateCount);

            direct.Launch();
            agent.HandlePackets();
            agent.HandlePackets();
            Assert.AreEqual(2, entry.UpdateCount);

            direct.Shutdown();
            agent.HandlePackets();
            Assert.AreEqual(2, entry.UpdateCount);

            IDisposable disposable = direct;
            disposable.Dispose();
        }

        [Test, Timeout(10000)]
        public void DirectInvocationSharesInstanceTest()
        {
            var chat = new DirectChat();
            var entry = new DirectEntry();
            entry.OpenedHandler = binder => binder.Bind<IDirectChat>(chat);

            var direct = new DirectStandalone(entry);
            PinionCore.Remote.Ghost.IAgent agent = direct;

            IDirectChat supplied = null;
            agent.QueryNotifier<IDirectChat>().Supply += c => supplied = c;

            direct.Launch();
            agent.HandleMessages();

            // 方法呼叫是直接的 .NET 呼叫：Value<T> 立即有值，不需 pump
            var result = 0;
            supplied.Add(1, 2).OnValue += (value, error) => result = value;
            Assert.AreEqual(3, result);

            // 事件也是直接訂閱：Soul 端觸發，客戶端立即收到
            string received = null;
            supplied.MessageEvent += message => received = message;
            chat.Broadcast("hello");
            Assert.AreEqual("hello", received);

            IDisposable disposable = direct;
            disposable.Dispose();
        }
    }
}
