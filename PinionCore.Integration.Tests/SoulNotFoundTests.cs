using System;
using System.Linq;
using NUnit.Framework;
using PinionCore.Remote;
using PinionCore.Remote.Server;
using PinionCore.Remote.Tools.Protocol.Sources.TestCommon;

namespace PinionCore.Integration.Tests
{
    public class SoulNotFoundTests
    {
        [NUnit.Framework.Test, Timeout(20000)]
        public void SoulNotFoundErrorContainsMethodNameAndCallSiteTest()
        {
            var logs = new System.Collections.Concurrent.ConcurrentQueue<string>();
            PinionCore.Utility.Log.RecordCallback recorder = line => logs.Enqueue(line);
            PinionCore.Utility.Log.Instance.RecordEvent += recorder;
            try
            {
                var protocol = ProtocolProvider.CreateCase1();
                var entry = NSubstitute.Substitute.For<IEntry>();
                var tester = new EchoTester(1);
                ISessionBinder binder = null;
                ISoul soul = null;
                entry.OnSessionOpened(NSubstitute.Arg.Do<ISessionBinder>(b =>
                {
                    binder = b;
                    soul = b.Bind<Echoable>(tester);
                }));

                PinionCore.Remote.Soul.IService service = new PinionCore.Remote.Soul.Service(entry, protocol);
                var ghostAgent = new PinionCore.Remote.Ghost.Agent(protocol);
                ghostAgent.RpcCallerStackTraceEnabled = true;
                PinionCore.Remote.Ghost.IAgent agent = ghostAgent;

                var endpoint = new PinionCore.Remote.Standalone.ListeningEndpoint();
                var (listenHandle, listenErrors) = service.ListenAsync(endpoint).GetAwaiter().GetResult();
                Assert.IsEmpty(listenErrors, "Standalone listener failed.");
                PinionCore.Remote.Client.IConnectingEndpoint connectable = endpoint;
                var stream = connectable.ConnectAsync().GetAwaiter().GetResult();
                ghostAgent.Enable(stream);

                Echoable echo = null;
                agent.QueryNotifier<Echoable>().Supply += g => echo = g;
                while (echo == null)
                {
                    agent.HandleMessages();
                    agent.HandlePackets();
                }

                // server 端先解綁,client 對過期 ghost 發 RPC → Soul not found 競態的確定性重現
                binder.Unbind(soul);

                PinionCore.Remote.Value<int> result = echo.Echo();
                string error = null;
                result.OnValue += (val, err) => error = err;
                while (error == null)
                {
                    agent.HandleMessages();
                    agent.HandlePackets();
                }

                StringAssert.StartsWith($"{nameof(Echoable)}.{nameof(Echoable.Echo)}: Soul not found entity_id:", error);
                StringAssert.Contains("return_id:", error);
                StringAssert.Contains("--- RPC call site", error);
                StringAssert.Contains(nameof(SoulNotFoundErrorContainsMethodNameAndCallSiteTest), error);

                // server log 為非同步寫出,輪詢等待 bound → unbound → not found 時間線齊備
                bool TimelineReady() =>
                    logs.Any(l => l.Contains("Soul bound entity_id:")) &&
                    logs.Any(l => l.Contains("Soul unbound entity_id:")) &&
                    logs.Any(l => l.Contains("Soul not found entity_id:") && l.Contains($"method:{nameof(Echoable)}.{nameof(Echoable.Echo)}"));

                var watch = System.Diagnostics.Stopwatch.StartNew();
                while (!TimelineReady() && watch.ElapsedMilliseconds < 10000)
                {
                    System.Threading.Thread.Sleep(10);
                }
                Assert.IsTrue(TimelineReady(), $"Soul lifecycle log timeline incomplete:\n{string.Join("\n", logs)}");

                ghostAgent.Disable();
                listenHandle.Dispose();
                IDisposable endpointDisposable = endpoint;
                endpointDisposable.Dispose();
                IDisposable serviceDisposable = service;
                serviceDisposable.Dispose();
            }
            finally
            {
                PinionCore.Utility.Log.Instance.RecordEvent -= recorder;
            }
        }
    }
}
