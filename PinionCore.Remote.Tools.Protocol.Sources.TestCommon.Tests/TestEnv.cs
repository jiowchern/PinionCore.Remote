using System;
using System.Diagnostics;
using System.Linq;
using PinionCore.Remote.Standalone;
using PinionCore.Utility;

namespace PinionCore.Remote.Tools.Protocol.Sources.TestCommon.Tests
{
    public class TestEnv<T, T2> where T : PinionCore.Remote.IEntry, System.IDisposable
    {

        public readonly INotifierQueryable Queryable;
        public readonly T Entry;

        readonly System.Action _Dispose;
        readonly Stopwatch _CounterMessage;
        readonly Stopwatch _CounterPackage;
        TimeSpan _NowMessage;
        TimeSpan _NowPackage;

        public TestEnv(T entry, TimeSpan expiry)
        {
            _CounterMessage = Stopwatch.StartNew();
            _CounterPackage = Stopwatch.StartNew();
            Entry = entry;
            IProtocol protocol = PinionCore.Remote.Protocol.ProtocolProvider.Create(typeof(T2).Assembly).Single();
            var ser = new PinionCore.Remote.Serializer(protocol.SerializeTypes);
            var internalSer = new PinionCore.Remote.InternalSerializer();
            #region standalone
            Service service = PinionCore.Remote.Standalone.Provider.CreateService(entry, protocol, ser, Memorys.PoolProvider.Shared);


            Ghost.IAgent agent = service.Create(new Stream());

            var updateMessage = new ThreadUpdater(() =>
            {
                //stream.Receive.Digestion();
                agent.HandleMessage();
                _NowMessage += _CounterMessage.Elapsed;
                _CounterMessage.Restart();
                if (_NowMessage > expiry)
                {
                    throw new TimeoutException("Message processing timed out");
                }
            });

            var updatePacket = new ThreadUpdater(() =>
            {
                //stream.Send.Digestion();
                agent.HandlePackets();
                _NowPackage += _CounterPackage.Elapsed;
                _CounterPackage.Restart();
                if (_NowPackage > expiry)
                {
                    throw new TimeoutException("Package processing timed out");
                }
            });
            _Dispose = () =>
            {
                Entry.Dispose();
                updateMessage.Stop();
                updatePacket.Stop();
                service.Destroy(agent);
                service.Dispose();
            };
            #endregion
            Queryable = agent;
            updatePacket.Start();
            updateMessage.Start();
        }

        public void Dispose()
        {
            _Dispose();


        }
    }
}
