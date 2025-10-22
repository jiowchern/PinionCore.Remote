using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework.Internal.Execution;
using PinionCore.Remote.Ghost;
using PinionCore.Remote.Reactive;
using PinionCore.Remote.Tools.Protocol.Sources.TestCommon;

namespace PinionCore.Remote.Gateway.Tests
{
    class TestClient : IDisposable
    {
        public readonly IAgent Agent;
        public TestClient(IProtocol protocol)
        {
            var agent = new Agent(new Hosts.AgentPool(protocol));
            Agent = agent;

        }
        public void Dispose()
        {            
        }

        internal async Task<int> GetMethod1()
        {
            var obs = from m in Agent.QueryNotifier<IMethodable1>().SupplyEvent()
                      from v in m.GetValue1().RemoteValue()
                      select v;
            return await obs.FirstAsync();
        }

        internal async Task<int> GetMethod2()
        {
            var obs = from m in Agent.QueryNotifier<IMethodable2>().SupplyEvent()
                      from v in m.GetValue2().RemoteValue()
                      select v;
            return await obs.FirstAsync();
        }

        internal async Task<bool> GetLogin()
        {
            var obs = from m in Agent.QueryNotifier<Consoles.Chat1.Common.ILogin>().SupplyEvent()
                      from v in m.Login("TestUser").RemoteValue()
                      select v;
            return await obs.FirstAsync();
        }
    }
}
