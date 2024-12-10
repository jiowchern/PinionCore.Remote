using System.Linq;
using System.Net;
using PinionCore.Network.Tcp;
using PinionCore.Remote.Standalone;

namespace PinionCore.Remote.Tools.Protocol.Sources.TestCommon.Tests
{
    public class TestEnv<T, T2> where T : PinionCore.Remote.IEntry, System.IDisposable
    {
        
        public readonly INotifierQueryable Queryable;
        public readonly T Entry;

        System.Action _Dispose;

        public TestEnv(T entry)
        {

            Entry = entry;
            IProtocol protocol = PinionCore.Remote.Protocol.ProtocolProvider.Create(typeof(T2).Assembly).Single();
            var ser = new PinionCore.Remote.Serializer(protocol.SerializeTypes);
            var internalSer = new PinionCore.Remote.InternalSerializer();

            //var service = PinionCore.Remote.Standalone.Provider.CreateService(entry, protocol, ser, Memorys.PoolProvider.Shared);
            //var agent = service.Create();
            var port= PinionCore.Network.Tcp.Tools.GetAvailablePort();
            var service = PinionCore.Remote.Server.Provider.CreateTcpService(entry, protocol);
            service.Listener.Bind(port);
            
            var client = PinionCore.Remote.Client.Provider.CreateTcpAgent(protocol);
            var agent = client.Agent;
            var peer = client.Connector.Connect(new IPEndPoint(IPAddress.Loopback, port)).GetAwaiter().GetResult();
            if(peer == null)
                throw new System.Exception("Connection failed");

            Queryable = agent;
            agent.Enable(peer);
            var updateMessage = new ThreadUpdater(() => {
                agent.HandleMessage();
            });

            var updatePacket = new ThreadUpdater(() => {
                agent.HandlePackets();
            });

            _Dispose = () =>
            {
                agent.Disable();
                service.Service.Dispose();
                Entry.Dispose();
                updateMessage.Stop();
                updatePacket.Stop();
                service.Listener.Close();
                client.Connector.Disconnect();
            };

            /*_Dispose = () =>
            {
                Entry.Dispose();
                updateMessage.Stop();
                updatePacket.Stop();
                service.Destroy(agent);
                service.Dispose();
            };*/

            updatePacket.Start();
            updateMessage.Start();
        }




        public void Dispose()
        {
            _Dispose();
            

        }
    }
}
