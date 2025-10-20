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
    class TestGameClient : IDisposable
    {
        readonly IAgent _Agent;
        public TestGameClient(IAgent agent)
        {
            _Agent = agent;

        }
        public void Dispose()
        {            
        }

        internal async Task<int> GetMethod()
        {
            var obs = from m in _Agent.QueryNotifier<IMethodable1>().SupplyEvent()
                      from v in m.GetValue1().RemoteValue()
                      select v;
            return await obs.FirstAsync();
        }
    }
}
