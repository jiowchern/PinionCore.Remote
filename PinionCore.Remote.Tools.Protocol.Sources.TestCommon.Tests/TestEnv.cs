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

        public event Action<Exception> ExceptionEvent;

        public TestEnv(T entry, TimeSpan expiry)
        {
            _CounterMessage = Stopwatch.StartNew();
            _CounterPackage = Stopwatch.StartNew();
            Entry = entry;
            IProtocol protocol = PinionCore.Remote.Protocol.ProtocolProvider.Create(typeof(T2).Assembly).Single();
            var serializer = new PinionCore.Remote.Serializer(protocol.SerializeTypes);
            #region standalone
            Service service = PinionCore.Remote.Standalone.Provider.CreateService(entry, protocol, serializer, new InternalSerializer() , Memorys.PoolProvider.Shared);


            Ghost.IAgent agent = PinionCore.Remote.Standalone.Provider.CreateAgent(protocol, serializer, new InternalSerializer(), Memorys.PoolProvider.Shared);
            var agentDisconnect = agent.Connect(service);

            var updateMessage = new ThreadUpdater(() =>
            {
                //stream.Receive.Digestion();
                agent.HandleMessage();
                _NowMessage += _CounterMessage.Elapsed;
                _CounterMessage.Restart();
                if (_NowMessage > expiry)
                {
                    ExceptionEvent?.Invoke(new TimeoutException("Message processing timed out"));
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
                    ExceptionEvent?.Invoke(new TimeoutException("Message processing timed out"));
                    throw new TimeoutException("Package processing timed out");
                }
            });
            _Dispose = () =>
            {
                Entry.Dispose();
                updateMessage.Stop();
                updatePacket.Stop();
                agentDisconnect();
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
