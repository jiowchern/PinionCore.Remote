using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using NUnit.Framework;
using PinionCore.Remote.Reactive;

namespace PinionCore.Remote.Tools.Protocol.Sources.TestCommon.Tests
{
    public class SpiritTests
    {
        [Test, Timeout(30000)]
        public async Task SpiritSupplyAndUnsupplyTest()
        {
            var tester = new Spirit1Tester();
            var env = new TestEnv<Entry<ISpirit1>, ISpirit1>(new Entry<ISpirit1>(tester), TimeSpan.FromSeconds(10));

            // 階段一：Spirit 供給 ghost 並可呼叫其方法
            var obs1 = from spirit1 in env.Queryable.QueryNotifier<ISpirit1>().SupplyEvent()
                       from m0 in spirit1.Get1().SupplyEvent()
                       from m0Value in m0.GetValue1().RemoteValue()
                       from m1 in spirit1.Get2(1).SupplyEvent()
                       from m1Value in m1.GetValue1().RemoteValue()
                       select new { m0Value, m1Value };

            var getValues = await obs1.FirstAsync();

            Assert.AreEqual(0, getValues.m0Value);
            Assert.AreEqual(1, getValues.m1Value);

            // 階段二：Soul 端 Dispose 觸發 Ghost 端 Unsupply
            var ghost = await env.Queryable.QueryNotifier<ISpirit1>().SupplyEvent().FirstAsync();
            Spirit<IMethodable1> spiritA = ghost.Get1();
            await spiritA.SupplyEvent().FirstAsync();
            Task<IMethodable1> unsupplyTask = spiritA.UnsupplyEvent().FirstAsync().ToTask();

            tester.TestDispose();
            await unsupplyTask;

            // 階段三：Dispose 後的呼叫永不供給
            var suppliedAfterDispose = false;
            Spirit<IMethodable1> spiritAfterDispose = ghost.Get2(1);
            spiritAfterDispose.SupplyEvent().Subscribe(_ => suppliedAfterDispose = true);
            await Task.Delay(TimeSpan.FromSeconds(1));
            Assert.IsFalse(suppliedAfterDispose);

            env.Dispose();
        }
    }
}
