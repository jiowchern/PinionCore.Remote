using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using PinionCore.Memorys;
using PinionCore.Network;
using PinionCore.Remote.Gateway.Sessions;

namespace PinionCore.Remote.Gateway.Tests
{
    public class GatewayServerTests
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(3);

        [Test, Timeout(10000)]
        public void Join_CreatesSingleSessionPerConnector()
        {
            var pool = PoolProvider.Shared;
            RunWithChannel(pool, channel =>
            {
                var server = new PinionCore.Remote.Gateway.Server();
                var connector = channel.SpawnConnector(new Stream());
                try
                {
                    server.Register(1, connector);

                    var enterCount = 0;
                    channel.Listenable.StreamableEnterEvent += _ => Interlocked.Increment(ref enterCount);

                    var userStream = new BufferRelay();
                    server.Join(userStream);

                    Assert.IsTrue(PumpUntil(new[] { connector }, () => Volatile.Read(ref enterCount) >= 1, DefaultTimeout), "User failed to attach to the initial session.");
                    PumpConnectors(new[] { connector }, TimeSpan.FromMilliseconds(200));

                    Assert.AreEqual(1, Volatile.Read(ref enterCount));
                }
                finally
                {
                    ((IDisposable)server).Dispose();
                    connector.Dispose();
                }
            });
        }

        [Test, Timeout(10000)]
        public void Register_AfterJoin_NotifiesExistingUser()
        {
            var pool = PoolProvider.Shared;
            RunWithChannel(pool, channel =>
            {
                var server = new PinionCore.Remote.Gateway.Server();
                var firstConnector = channel.SpawnConnector(new Stream());
                GatewaySessionConnector secondConnector = null;
                try
                {
                    server.Register(1, firstConnector);

                    var enterCount = 0;
                    channel.Listenable.StreamableEnterEvent += _ => Interlocked.Increment(ref enterCount);

                    var userStream = new BufferRelay();
                    server.Join(userStream);
                    Assert.IsTrue(PumpUntil(new[] { firstConnector }, () => Volatile.Read(ref enterCount) >= 1, DefaultTimeout), "User failed to attach to the first session.");

                    secondConnector = channel.SpawnConnector(new Stream());
                    server.Register(2, secondConnector);

                    Assert.IsTrue(PumpUntil(new[] { firstConnector, secondConnector }, () => Volatile.Read(ref enterCount) >= 2, DefaultTimeout), $"Expected session count 2 but was {Volatile.Read(ref enterCount)}.");
                    PumpConnectors(new[] { firstConnector, secondConnector }, TimeSpan.FromMilliseconds(200));

                    Assert.AreEqual(2, Volatile.Read(ref enterCount));
                }
                finally
                {
                    ((IDisposable)server).Dispose();
                    firstConnector.Dispose();
                    secondConnector?.Dispose();
                }
            });
        }

        [Test, Timeout(10000)]
        public void Unregister_PropagatesLeaveToActiveUsers()
        {
            var pool = PoolProvider.Shared;
            RunWithChannel(pool, channel =>
            {
                var server = new PinionCore.Remote.Gateway.Server();
                var connector = channel.SpawnConnector(new Stream());
                try
                {
                    server.Register(1, connector);

                    var enterCount = 0;
                    var leaveCount = 0;
                    channel.Listenable.StreamableEnterEvent += _ => Interlocked.Increment(ref enterCount);
                    channel.Listenable.StreamableLeaveEvent += _ => Interlocked.Increment(ref leaveCount);

                    var userStream = new BufferRelay();
                    server.Join(userStream);

                    Assert.IsTrue(PumpUntil(new[] { connector }, () => Volatile.Read(ref enterCount) >= 1, DefaultTimeout), "User failed to attach before unregister.");

                    server.Unregister(connector);

                    Assert.IsTrue(PumpUntil(new[] { connector }, () => Volatile.Read(ref leaveCount) >= 1, DefaultTimeout), $"Expected leave count 1 but was {Volatile.Read(ref leaveCount)}.");

                    Assert.AreEqual(1, Volatile.Read(ref leaveCount));
                }
                finally
                {
                    ((IDisposable)server).Dispose();
                    connector.Dispose();
                }
            });
        }

        [Test, Timeout(10000)]
        public void Leave_RemovesAllSessionsForUser()
        {
            var pool = PoolProvider.Shared;
            RunWithChannel(pool, channel =>
            {
                var server = new PinionCore.Remote.Gateway.Server();
                var connector = channel.SpawnConnector(new Stream());
                try
                {
                    server.Register(1, connector);

                    var enterCount = 0;
                    var leaveCount = 0;
                    channel.Listenable.StreamableEnterEvent += _ => Interlocked.Increment(ref enterCount);
                    channel.Listenable.StreamableLeaveEvent += _ => Interlocked.Increment(ref leaveCount);

                    var userStream = new BufferRelay();
                    server.Join(userStream);

                    Assert.IsTrue(PumpUntil(new[] { connector }, () => Volatile.Read(ref enterCount) >= 1, DefaultTimeout), "User failed to attach before leave.");

                    server.Leave(userStream);

                    Assert.IsTrue(PumpUntil(new[] { connector }, () => Volatile.Read(ref leaveCount) >= 1, DefaultTimeout), $"Expected leave count 1 but was {Volatile.Read(ref leaveCount)}.");

                    Assert.AreEqual(1, Volatile.Read(ref leaveCount));
                }
                finally
                {
                    ((IDisposable)server).Dispose();
                    connector.Dispose();
                }
            });
        }

        private static bool PumpUntil(IEnumerable<GatewaySessionConnector> connectors, Func<bool> predicate, TimeSpan timeout)
        {
            var stopwatch = Stopwatch.StartNew();
            var spin = new SpinWait();

            while (!predicate())
            {
                if (stopwatch.Elapsed > timeout)
                {
                    return false;
                }

                spin.SpinOnce();
            }

            return true;
        }

        private static void PumpConnectors(IEnumerable<GatewaySessionConnector> connectors, TimeSpan duration)
        {
            Thread.Sleep(duration);
        }

        private static void RunWithChannel(IPool pool, Action<SessionChannel> action)
        {
            var channel = new SessionChannel(pool);
            try
            {
                action(channel);
            }
            finally
            {
                channel.Dispose();
            }
        }
    }
}