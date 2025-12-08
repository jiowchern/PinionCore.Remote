using System;
using System.Reactive.Linq;
using System.Threading;
using NUnit.Framework;
using PinionCore.Remote.Gateway.Hosts;
using PinionCore.Remote.Gateway.Registrys;
using PinionCore.Remote.Reactive;

using PinionCore.Remote.Standalone;
using PinionCore.Remote.Client;
using PinionCore.Remote.Server;
using PinionCore.Utility;


namespace PinionCore.Remote.Gateway.Tests
{
    public class RegistrysTests
    {
       

        [NUnit.Framework.Test, Timeout(10000)]
        public async System.Threading.Tasks.Task Server_AllocatesStreamsPerGroupAsync()
        {
            var version = PinionCore.Remote.Gateway.Protocols.ProtocolProvider.Create().VersionCode;
            var server = new PinionCore.Remote.Gateway.Registrys.Server();
            using var service = server.ToService();

            var client1 = new PinionCore.Remote.Gateway.Registrys.Client(1, version);
            var client2 = new PinionCore.Remote.Gateway.Registrys.Client(2, version);
            var dispose1 = ConnectAgent(client1.Agent, service);
            var dispose2 = ConnectAgent(client2.Agent, service);

            using var worker1 = new AgentWorker(client1.Agent);


            using var worker2 = new AgentWorker(client2.Agent);
            


            
            // 使用 TaskCompletionSource 避免競爭條件
            var client1Tcs = new System.Threading.Tasks.TaskCompletionSource<Network.IStreamable>();
            var client2Tcs = new System.Threading.Tasks.TaskCompletionSource<Network.IStreamable>();

            void Client1Handler(Network.IStreamable stream) => client1Tcs.TrySetResult(stream);
            void Client2Handler(Network.IStreamable stream) => client2Tcs.TrySetResult(stream);

            client1.Listener.StreamableEnterEvent += Client1Handler;
            client2.Listener.StreamableEnterEvent += Client2Handler;

            try
            {
                var serverRequire1Obs = from s in server.LinesNotifier.SupplyEvent()
                                        where s.Group == 1
                                        select s;

                var alloc1 = await serverRequire1Obs.FirstAsync();

                var serverStream1 = alloc1.Alloc();


                var serverRequire2Obs = from s in server.LinesNotifier.SupplyEvent()
                                        where s.Group == 2
                                        select s;
                var alloc2 = await serverRequire2Obs.FirstAsync();
                var serverStream2 = alloc2.Alloc();


                var clientStream1 = await client1Tcs.Task;
                var clientStream2 = await client2Tcs.Task;
            //TestContext.Progress.WriteLine("Client stream 2 supplied.");


            var sendData1 = new byte[] { 1, 2, 3, 4, 5 };
            var count1 = await clientStream1.Send(sendData1, 0, sendData1.Length, CancellationToken.None);
            Assert.AreEqual(sendData1.Length, count1);

            var receiveBuffer1 = new byte[5];
            var count2 = await serverStream1.Receive(receiveBuffer1, 0, receiveBuffer1.Length, CancellationToken.None);
            Assert.AreEqual(sendData1.Length, count2);
            // check data integrity
            Assert.AreEqual(sendData1, receiveBuffer1);

            var sendData2 = new byte[] { 6, 7, 8, 9, 10 };
            var count3 = await clientStream2.Send(sendData2, 0, sendData2.Length, CancellationToken.None);
            Assert.AreEqual(sendData2.Length, count3);
            var receiveBuffer2 = new byte[5];
            var count4 = await serverStream2.Receive(receiveBuffer2, 0, receiveBuffer2.Length, CancellationToken.None);
            Assert.AreEqual(sendData2.Length, count4);
            // check data integrity
            Assert.AreEqual(sendData2, receiveBuffer2);
            }
            finally
            {
                client1.Listener.StreamableEnterEvent -= Client1Handler;
                client2.Listener.StreamableEnterEvent -= Client2Handler;
            }

            dispose1.Dispose();
            dispose2.Dispose();
        }
        private static IDisposable ConnectAgent(Remote.Ghost.IAgent agent, PinionCore.Remote.Soul.IService service)
        {
            var endpoint = new PinionCore.Remote.Standalone.ListeningEndpoint();
            var (handle, errors) = service.ListenAsync(endpoint).GetAwaiter().GetResult();
            if (errors.Length > 0)
            {
                throw new InvalidOperationException($"Standalone listener failed: {errors[0].Exception}");
            }

            PinionCore.Remote.Client.IConnectingEndpoint connectable = endpoint;
            var stream = connectable.ConnectAsync().GetAwaiter().GetResult();
            agent.Enable(stream);

            return new DisposeAction(() =>
            {
                agent.Disable();
                handle.Dispose();
                IDisposable disposable = endpoint;
                disposable.Dispose();
            });
        }
    }
}


