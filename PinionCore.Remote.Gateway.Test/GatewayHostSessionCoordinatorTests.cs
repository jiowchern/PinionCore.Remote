using System.Threading;
using NUnit.Framework;
using PinionCore.Remote.Gateway.Hosts;
using PinionCore.Remote.Gateway.Protocols;
using PinionCore.Remote.Gateway.Servers;
using PinionCore.Remote.Reactive;
using System.Linq;
using System.Reactive.Linq;



namespace PinionCore.Remote.Gateway.Tests
{
    public class GatewayHostSessionCoordinatorTests
    {
        [NUnit.Framework.Test, Timeout(10000)]
        public async System.Threading.Tasks.Task SessionCoordinator_ConnectionRegisterJoinUnregister_Succeeds()
        {
            var coordinator = new PinionCore.Remote.Gateway.Hosts.GatewayHostSessionCoordinator(new RoundRobinGameLobbySelectionStrategy());
            IServiceRegistry registry = coordinator;
            ISessionMembership membership = coordinator;

            var m1 = new GatewayServerConnectionManager();
            var m1Disposer = new ClientConnectionDisposer(new RoundRobinGameLobbySelectionStrategy());
            registry.Register(1, m1Disposer);

            var conn1 = new GatewayHostConnectionManager();
            IConnectionManager connectionManager = conn1;
            var supplyObs = from c in connectionManager.Connections.Base.SupplyEvent()                      
                      select c;

            var unsupplyObs = from c in connectionManager.Connections.Base.UnsupplyEvent()
                            select c;

            membership.Join(conn1);

            var c1 = await supplyObs.FirstAsync();
            Assert.AreEqual(1, c1.Id.Value);

            m1Disposer.Remove(m1);
            registry.Unregister(m1Disposer);
            var c2 = await unsupplyObs.FirstAsync();

            Assert.AreEqual(1, c2.Id.Value);
            coordinator.Dispose();
        }
    }
}



