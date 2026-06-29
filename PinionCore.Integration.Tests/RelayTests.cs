using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PinionCore.Remote;
using PinionCore.Remote.Client;
using PinionCore.Remote.Server;
using PinionCore.Remote.Reactive;
using TestCommon = PinionCore.Remote.Tools.Protocol.Sources.TestCommon;

namespace PinionCore.Integration.Tests
{
    /// <summary>
    /// 跨服務器介面轉傳（Cross-Server Interface Relay）範例。
    ///
    /// 情境：
    ///   - B 服務器產生某個介面實體。
    ///   - A 服務器同時扮演兩種角色：
    ///       * 對 B 而言是「客戶端」（Agent），連到 B 並取得介面的 ghost 代理。
    ///       * 對 Client 而言是「服務器」（Host），把剛剛收到的 ghost 直接 Bind 出去。
    ///   - Client 只連接 A，從未連接 B，卻能取得並使用源自 B 的介面。
    ///
    /// 之所以能成立，是因為傳輸單位是「介面」：ghost 本身就實作了該介面，
    /// 所以 A 可以把它當成自己的實作再 Bind 一次。只要介面來自同一份 IProtocol，
    /// 就能在服務器之間任意轉傳，而且 Method / Property / Notifier 都能透通運作。
    /// </summary>
    public class RelayTests
    {
        // 每個測試的逾時上限（毫秒），避免轉傳失敗時卡住。
        private const int TimeoutMs = 5000;

        /// <summary>
        /// Method 轉傳：B 上的 GetValue1 回傳 999，Client 只連 A 也能呼叫到。
        /// </summary>
        [Test, Timeout(TimeoutMs)]
        public async Task RelayMethodAcrossServersTest()
        {
            var protocol = TestCommon.ProtocolProvider.CreateCase1();
            var serverBImpl = new ServerBMethodable();

            using var harness = new RelayHarness<TestCommon.IMethodable>(protocol, serverBImpl);

            var observable = from methodable in harness.ClientAgent.QueryNotifier<TestCommon.IMethodable>().SupplyEvent()
                             from value in methodable.GetValue1().RemoteValue()
                             select value;
            var result = await observable.FirstAsync();

            // 999 源自 B，但 Client 從頭到尾只連接 A。
            Assert.AreEqual(999, result);
        }

        /// <summary>
        /// Property 轉傳：Client 透過 A 讀到 B 上 Property 的初始狀態，
        /// 且 B 端「之後」的 Property 變更也會一路同步到 Client（B → A → Client）。
        ///
        /// 能即時轉傳的關鍵：ghost 套用網路更新走 Property&lt;T&gt; 的 IAccessable.Set()，
        /// 該路徑會觸發 DirtyEvent，因此中繼節點 A 的 SoulProxy 能觀察到變更並再轉發。
        /// </summary>
        [Test, Timeout(TimeoutMs)]
        public void RelayPropertyAcrossServersTest()
        {
            var protocol = TestCommon.ProtocolProvider.CreateCase1();
            var serverBImpl = new TestCommon.PropertyTester();

            using var harness = new RelayHarness<TestCommon.IPropertyable>(protocol, serverBImpl);

            var ghostObservable = from gpi in harness.ClientAgent.QueryNotifier<TestCommon.IPropertyable>().SupplyEvent()
                                  select gpi;
            TestCommon.IPropertyable ghost = ghostObservable.FirstAsync().Wait();

            // 初始值（B 端 Property1 = 1、Property2 = 2）經由 A 同步到只連 A 的 Client。
            SpinWait.SpinUntil(() => ghost.Property1.Value == 1 && ghost.Property2.Value == 2, 3000);
            Assert.AreEqual(1, ghost.Property1.Value);
            Assert.AreEqual(2, ghost.Property2.Value);

            // 在 B 端更新 Property，變化應一路同步：B → A → Client。
            serverBImpl.Property1.Value++;
            serverBImpl.Property2.Value++;

            SpinWait.SpinUntil(() => ghost.Property1.Value == 2 && ghost.Property2.Value == 3, 3000);
            Assert.AreEqual(2, ghost.Property1.Value);
            Assert.AreEqual(3, ghost.Property2.Value);
        }

        /// <summary>
        /// Notifier 轉傳：B 端 Depot 供應的物件，Client 只連 A 也能收到整棵物件樹，
        /// 包含每個元素自身的 Property。
        /// </summary>
        [Test, Timeout(TimeoutMs)]
        public void RelayNotifierAcrossServersTest()
        {
            var protocol = TestCommon.ProtocolProvider.CreateCase1();
            var serverBImpl = new TestCommon.MultipleNotices.MultipleNotices();

            // B 端先放入兩個 Number（值分別為 1、2）。
            serverBImpl.Numbers1.Items.Add(new TestCommon.Number(1));
            serverBImpl.Numbers1.Items.Add(new TestCommon.Number(2));

            using var harness = new RelayHarness<TestCommon.MultipleNotices.IMultipleNotices>(protocol, serverBImpl);

            // Client 透過 A 觀察 Numbers1 供應出來的元素，並讀取每個元素的 Property 值。
            var numbersObservable = from mn in harness.ClientAgent.QueryNotifier<TestCommon.MultipleNotices.IMultipleNotices>().SupplyEvent()
                                    from number in mn.Numbers1.Base.SupplyEvent()
                                    select number.Value.Value;
            IList<int> numbers = numbersObservable.Buffer(2).FirstAsync().Wait();

            CollectionAssert.AreEquivalent(new[] { 1, 2 }, numbers);
        }

        /// <summary>
        /// B 服務器上的 IMethodable 實作。GetValue1 刻意回傳 999，作為「值確實來自 B」的標記；
        /// 其餘成員沿用最小實作，僅為滿足介面。
        /// </summary>
        private class ServerBMethodable : TestCommon.IMethodable
        {
            Value<int> TestCommon.IMethodable1.GetValue1()
            {
                return 999;
            }

            Value<int> TestCommon.IMethodable2.GetValue2()
            {
                return 2;
            }

            Value<TestCommon.HelloReply> TestCommon.IMethodable2.SayHello(TestCommon.HelloRequest request)
            {
                return new TestCommon.HelloReply() { Message = request.Name };
            }

            Value<int[]> TestCommon.IMethodable.GetValue0(int _1, string _2, float _3, double _4, decimal _5, Guid _6)
            {
                return new int[] { _1 };
            }

            Value<TestCommon.IMethodable> TestCommon.IMethodable.GetValueSelf()
            {
                return this;
            }

            Value TestCommon.IMethodable.MethodNoValue(TestCommon.TestStruct arg1)
            {
                return new Value(false);
            }

            int TestCommon.IMethodable.NotSupported()
            {
                return 0;
            }

            IAwaitableSource<int> TestCommon.IMethodable.StreamableMethod(byte[] buffer, int offset, int count, CancellationToken token)
            {
                return new PinionCore.Network.NoWaitValue<int>(count);
            }
        }

        /// <summary>
        /// 建立並維持 B → A → Client 的轉傳鏈，並在背景持續驅動兩個 Agent。
        /// 釋放時會停止背景驅動並關閉所有連線與 Host。
        /// </summary>
        private sealed class RelayHarness<T> : IDisposable where T : class
        {
            private readonly CancellationTokenSource _Cancellation = new CancellationTokenSource();
            private readonly Task _Pump;
            private readonly List<IDisposable> _Disposables = new List<IDisposable>();

            public PinionCore.Remote.Ghost.IAgent ClientAgent { get; }

            public RelayHarness(IProtocol protocol, T serverBImpl)
            {
                // ── B 服務器：產生介面實體 ────────────────────────────────────
                IEntry entryB = NSubstitute.Substitute.For<IEntry>();
                entryB.OnSessionOpened(NSubstitute.Arg.Do<ISessionBinder>(
                    binder => binder.Bind<T>(serverBImpl)));
                var serverB = new Host(entryB, protocol);
                _Disposables.Add(serverB);

                // ── A 服務器（角色一：當 B 的客戶端）─────────────────────────
                var agentAtoB = new Proxy(protocol);
                _Disposables.Add(ConnectStandalone(serverB, agentAtoB.Agent));

                // 同步驅動 A 的 Agent，直到 B 供應介面的 ghost。
                T relayedGhost = null;
                agentAtoB.Agent.QueryNotifier<T>().Supply += ghost => relayedGhost = ghost;
                while (relayedGhost == null)
                {
                    agentAtoB.Agent.HandlePackets();
                    agentAtoB.Agent.HandleMessages();
                }

                // ── A 服務器（角色二：當 Client 的服務器，轉傳 ghost）────────
                // 直接把收到的 ghost 當成自己的實作 Bind 出去 —— 不需任何包裝或 DTO。
                IEntry entryA = NSubstitute.Substitute.For<IEntry>();
                entryA.OnSessionOpened(NSubstitute.Arg.Do<ISessionBinder>(
                    binder => binder.Bind<T>(relayedGhost)));
                var serverA = new Host(entryA, protocol);
                _Disposables.Add(serverA);

                // ── Client：只連 A，從未連 B ──────────────────────────────────
                var client = new Proxy(protocol);
                _Disposables.Add(ConnectStandalone(serverA, client.Agent));
                ClientAgent = client.Agent;

                // 背景持續驅動兩個 Agent：
                //   client.Agent    ── 呼叫 A
                //   agentAtoB.Agent ── A 內部再把呼叫轉給 B
                // 每圈 Sleep(1ms) 讓出 CPU，避免緊迴圈在取消時無法即時退出。
                var token = _Cancellation.Token;
                _Pump = Task.Run(() =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        agentAtoB.Agent.HandlePackets();
                        agentAtoB.Agent.HandleMessages();
                        client.Agent.HandlePackets();
                        client.Agent.HandleMessages();
                        Thread.Sleep(1);
                    }
                }, token);
            }

            public void Dispose()
            {
                _Cancellation.Cancel();
                try
                {
                    _Pump.Wait(TimeSpan.FromSeconds(2));
                }
                catch (AggregateException)
                {
                    // 背景驅動在取消時可能擲出，屬預期，忽略。
                }

                // 反向釋放：先關 Client 端連線，再往回關到 B。
                for (int i = _Disposables.Count - 1; i >= 0; i--)
                {
                    _Disposables[i].Dispose();
                }

                _Cancellation.Dispose();
            }
        }

        /// <summary>
        /// 以 Standalone（記憶體內、免網路）方式，把一個 Agent 連到一個 Service。
        /// 回傳的 IDisposable 會在釋放時關閉連線。
        /// </summary>
        private static IDisposable ConnectStandalone(PinionCore.Remote.Soul.IService service, PinionCore.Remote.Ghost.IAgent agent)
        {
            var endpoint = new PinionCore.Remote.Standalone.ListeningEndpoint();
            var (handle, errors) = service.ListenAsync(endpoint).GetAwaiter().GetResult();
            if (errors.Length > 0)
            {
                throw new InvalidOperationException($"Standalone listener failed: {errors[0].Exception}");
            }

            PinionCore.Remote.Client.IConnectingEndpoint connectable = endpoint;
            var stream = connectable.ConnectAsync().GetAwaiter().GetResult();
            agent.Enable(stream);

            return new Disconnector(() =>
            {
                agent.Disable();
                handle.Dispose();
                IDisposable disposable = endpoint;
                disposable.Dispose();
            });
        }

        private sealed class Disconnector : IDisposable
        {
            private readonly Action _OnDispose;
            public Disconnector(Action onDispose)
            {
                _OnDispose = onDispose;
            }
            public void Dispose()
            {
                _OnDispose();
            }
        }
    }
}
