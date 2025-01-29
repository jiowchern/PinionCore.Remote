using System.Security.Cryptography;
using NUnit.Framework;
using PinionCore.Remote;
using PinionCore.Remote.Reactive;
using System.Linq;

using System.Reactive.Linq;
using System.Net.WebSockets;
using PinionCore.Remote.Tools.Protocol.Sources.TestCommon;
using System;
namespace PinionCore.Integration.Tests
{
    public class EchoTests
    {
        [Test]
        [NUnit.Framework.Timeout(1000)]
        [NUnit.Framework.TestCase(100)]
        public async System.Threading.Tasks.Task IntervalTest(int agent_count)
        {
            var protocol = PinionCore.Remote.Tools.Protocol.Sources.TestCommon.ProtocolProvider.CreateCase1();
            var entry = NSubstitute.Substitute.For<IEntry>();
            var tester = new PinionCore.Remote.Tools.Protocol.Sources.TestCommon.EchoTester();
            entry.RegisterClientBinder(NSubstitute.Arg.Do<IBinder>(b => b.Bind<PinionCore.Remote.Tools.Protocol.Sources.TestCommon.Echoable>(tester)));

            var service = PinionCore.Remote.Standalone.Provider.CreateService(entry, protocol);


            var agentsObs = from i in System.Reactive.Linq.Observable.Range(0, agent_count)
                            select service.Create(new Remote.Standalone.Stream());

            var agents = await agentsObs.ToList();

            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();

            var intervalsObs = from agent in agents.ToObservable()
                               let start = stopWatch.Elapsed
                               from echo in agent.QueryNotifier<Echoable>().SupplyEvent()
                               from value in echo.Echo(1).RemoteValue()
                               select stopWatch.Elapsed - start;
            var intervals = new System.Collections.Generic.List<TimeSpan>();
            intervalsObs.Subscribe(intervals.Add);

            while (intervals.Count() < agent_count)
            {
                foreach (var agent in agents)
                {
                    agent.HandleMessage();
                    agent.HandlePackets();
                }
            }

            var sumInterval = intervals.Sum(i => i.Ticks);
            var avgInterval = sumInterval / agent_count;
            System.Console.WriteLine(TimeSpan.FromTicks(avgInterval).TotalSeconds) ;

        }
    }
}
