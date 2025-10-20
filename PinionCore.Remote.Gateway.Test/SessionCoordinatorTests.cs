using System.Collections.Generic;
using NUnit.Framework;
using PinionCore.Network;
using PinionCore.Remote.Gateway.Hosts;
using PinionCore.Remote.Gateway.Protocols;
using PinionCore.Remote.Gateway.Registrys;

namespace PinionCore.Remote.Gateway.Tests
{
    public class SessionCoordinatorTests
    {
        [Test]
        public void RegisterAndJoin_AssignsStream()
        {
            var coordinator = new SessionCoordinator(new RoundRobinGameLobbySelectionStrategy());
            var roster = new ConnectionRoster();
            var rosterView = (IConnectionRoster)roster;
            var supplied = new List<IStreamable>();
            rosterView.Connections.Base.Supply += supplied.Add;

            var allocator = new StubAllocator(1);

            coordinator.Join(roster);
            coordinator.Register(1, allocator);

            Assert.That(supplied, Has.Count.EqualTo(1));
            Assert.That(allocator.AllocatedCount, Is.EqualTo(1));
        }

        [Test]
        public void Unregister_ReleasesStream()
        {
            var coordinator = new SessionCoordinator(new RoundRobinGameLobbySelectionStrategy());
            var roster = new ConnectionRoster();
            var allocator = new StubAllocator(2);

            coordinator.Join(roster);
            coordinator.Register(2, allocator);
            coordinator.Unregister(2, allocator);

            Assert.That(allocator.ReleasedCount, Is.EqualTo(allocator.AllocatedCount));
        }

        private sealed class StubAllocator : ILineAllocatable
        {
            private readonly uint _group;
            private readonly Queue<IStreamable> _pool = new Queue<IStreamable>();

            public StubAllocator(uint group)
            {
                _group = group;
                _pool.Enqueue(new PinionCore.Network.Stream());
                _pool.Enqueue(new PinionCore.Network.Stream());
            }

            public int AllocatedCount { get; private set; }
            public int ReleasedCount { get; private set; }

            public uint Group => _group;

            public IStreamable Alloc()
            {
                AllocatedCount++;
                return _pool.Count > 0 ? _pool.Dequeue() : new PinionCore.Network.Stream();
            }

            public void Free(IStreamable stream)
            {
                ReleasedCount++;
            }
        }
    }
}





