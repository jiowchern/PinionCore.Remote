using System.Security.Cryptography;
using NUnit.Framework;
using PinionCore.Remote;
using PinionCore.Remote.Reactive;
using System.Linq;

using System.Reactive.Linq;
using System.Net.WebSockets;
using PinionCore.Remote.Tools.Protocol.Sources.TestCommon;
using System;
using PinionCore.Remote.Standalone;
namespace PinionCore.Integration.Tests
{
    public class EchoTests
    {
        [Test]        
        [NUnit.Framework.TestCase(1)]
        [NUnit.Framework.TestCase(2)]
        [NUnit.Framework.TestCase(30)]
        [NUnit.Framework.TestCase(400)]
        public async System.Threading.Tasks.Task IntervalTest(int agent_count)
        {
            var protocol = PinionCore.Remote.Tools.Protocol.Sources.TestCommon.ProtocolProvider.CreateCase1();
            var entry = NSubstitute.Substitute.For<IEntry>();
            var tester = new PinionCore.Remote.Tools.Protocol.Sources.TestCommon.EchoTester();
            entry.RegisterClientBinder(NSubstitute.Arg.Do<IBinder>(b => b.Bind<PinionCore.Remote.Tools.Protocol.Sources.TestCommon.Echoable>(tester)));

            var service = PinionCore.Remote.Standalone.Provider.CreateService(entry, protocol);

            var agentsObs = from i in System.Reactive.Linq.Observable.Range(0, agent_count)
                            select PinionCore.Remote.Standalone.Provider.CreateAgent(protocol);

            var agents = await agentsObs.ToList();

            var agentDisconnects = (from a in agents
                                  select a.Connect(service)).ToArray();


            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            var intervalsObs = from agent in agents.ToObservable()
                               from echo in agent.QueryNotifier<Echoable>().SupplyEvent()//.Delay(TimeSpan.FromMilliseconds(new System.Random().Next(0, agent_count * 10)))
                               from start in Observable.Start(() => stopWatch.Elapsed)//.Do(s => System.Console.WriteLine($"start:{s}"))
                               from value in echo.Echo().RemoteValue()
                               from end in Observable.Start(() => stopWatch.Elapsed)//.Do(e => System.Console.WriteLine($"end:{e}"))
                               select end - start;

            var intervals = new System.Collections.Concurrent.ConcurrentBag<TimeSpan>();

            var updateTask = System.Threading.Tasks.Task.Run(() =>
            {
                while (intervals.Count() < agent_count)
                {
                    foreach (var agent in agents)
                    {
                        agent.HandlePackets();
                        agent.HandleMessage();
                        
                    }
                    //await System.Threading.Tasks.Task.Delay(1);
                }
            });


            intervalsObs.Subscribe(interval => intervals.Add(interval));


            updateTask.Wait();

            var sumInterval = intervals.Sum(i => i.Ticks);
            var maxInterval = intervals.Max(i => i.Ticks);
            var minInterval = intervals.Min(i => i.Ticks);
            var avgInterval = sumInterval / agent_count;

            foreach(var agentDisconnect in agentDisconnects)
            {
                agentDisconnect();
            }
            System.Console.WriteLine($"avg:{TimeSpan.FromTicks(avgInterval).TotalSeconds} min:{TimeSpan.FromTicks(minInterval).TotalSeconds} max:{TimeSpan.FromTicks(maxInterval).TotalSeconds} sum:{TimeSpan.FromTicks(sumInterval).TotalSeconds}") ;


        }
    }
}
