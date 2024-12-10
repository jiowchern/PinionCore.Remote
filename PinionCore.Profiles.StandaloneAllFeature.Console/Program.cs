using System.Diagnostics;
using System.Reactive.Linq;
using PinionCore.Remote.Reactive;

namespace PinionCore.Profiles.StandaloneAllFeature.Console
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Remote.IProtocol protocol = PinionCore.Profiles.StandaloneAllFeature.Protocols.ProtocolProvider.Create();
            var entry = new Server.Entry();
            var range = 10;





            var set = PinionCore.Remote.Server.Provider.CreateTcpService( entry, protocol);
            var port = PinionCore.Network.Tcp.Tools.GetAvailablePort();
            set.Listener.Bind(port);

            ProcessAgents( range, ()=>{
                var clientSet = PinionCore.Remote.Client.Provider.CreateTcpAgent(PinionCore.Profiles.StandaloneAllFeature.Protocols.ProtocolProvider.Create());

                var w = clientSet.Connector.Connect(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, port)).GetAwaiter();
                var peer = w.GetResult();
                clientSet.Agent.Enable(peer);
                return clientSet.Agent;
            });
            set.Service.Dispose();
            


            Remote.Standalone.Service service = PinionCore.Remote.Standalone.Provider.CreateService(entry, protocol);
            ProcessAgents(range, () =>
            {
                lock (service)
                    return service.Create();
            });



        }

        private static void ProcessAgents(int range, Func<PinionCore.Remote.Ghost.IAgent> agent_provider)
        {
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = 10,
            };

            var ticks = 0L;
            for (var i = 1; i <= range; i++)
            {

                System.Threading.Tasks.Parallel.For(0, 10, options, index =>
                {
                    var user = new User(agent_provider(), i * (index + 1));
                    Remote.Ghost.IAgent agent = user.Agent;

                    IObservable<string> obs = from e in agent.QueryNotifier<PinionCore.Profiles.StandaloneAllFeature.Protocols.Featureable>().SupplyEvent()
                                              from num in System.Reactive.Linq.Observable.Range(0, range)
                                              from v in e.Inc(System.Guid.NewGuid().ToString() + System.Guid.NewGuid().ToString() + System.Guid.NewGuid().ToString() + System.Guid.NewGuid().ToString() + System.Guid.NewGuid().ToString() + System.Guid.NewGuid().ToString() + System.Guid.NewGuid().ToString() + System.Guid.NewGuid().ToString() + System.Guid.NewGuid().ToString() + System.Guid.NewGuid().ToString() + System.Guid.NewGuid().ToString() + System.Guid.NewGuid().ToString()).RemoteValue()
                                              select v;
                    var enable = true;

                    IObservable<IList<string>> bufferObs = obs.Buffer(range);
                    var stopWatch = new Stopwatch();
                    stopWatch.Restart();
                    System.Console.WriteLine($"Start {user.Id}/{range * 10}");
                    bufferObs.Subscribe(v =>
                    {
                        stopWatch.Stop();
                        user.Ticks = stopWatch.ElapsedTicks;
                        enable = false;
                    });

                    long sleepCount = 0;
                    while (enable)
                    {
                        agent.HandlePackets();
                        agent.HandleMessage();
                        var sw = Stopwatch.StartNew();
                        System.Threading.Tasks.Task.Delay(range).Wait();
                        sleepCount += sw.ElapsedTicks;
                    }

                    agent.Disable();
                    user.Ticks = user.Ticks - sleepCount;
                    var time = new TimeSpan(user.Ticks / range);
                    System.Console.WriteLine($"Done {user.Id}/{range} time:{time}");

                    System.Threading.Interlocked.Add(ref ticks, user.Ticks);
                });

            }





            var average = new TimeSpan(ticks / range / range);
            System.Console.WriteLine($"Average time : {average} ({average.TotalMilliseconds}ms)");
            System.Console.WriteLine($"Total time : {new TimeSpan(ticks)}");

            //service.Dispose();

            IReadOnlyCollection<Memorys.Chankable> chunks = PinionCore.Memorys.PoolProvider.Shared.Chunks;
            foreach (Memorys.Chankable? chunk in chunks)
            {
                System.Console.WriteLine($"Remote Chunk : {chunk.BufferSize} {chunk.AvailableCount} {chunk.DefaultAllocationThreshold} {chunk.PageSize}");
            }
        }
    }
}
