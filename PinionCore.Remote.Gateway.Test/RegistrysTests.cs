
using System;
using System.Reactive.Linq;
using NUnit.Framework;
using PinionCore.Remote.Gateway.Hosts;
using PinionCore.Remote.Gateway.Registrys;
using PinionCore.Remote.Reactive;

using PinionCore.Remote.Standalone;


namespace PinionCore.Remote.Gateway.Tests
{
    public class RegistrysTests
    {
       

        [NUnit.Framework.Test, Timeout(10000)]
        public async System.Threading.Tasks.Task Server_AllocatesStreamsPerGroupAsync()
        {
            var server = new PinionCore.Remote.Gateway.Registrys.Server();
            using var service = server.ToService();

            var client1 = new PinionCore.Remote.Gateway.Registrys.Client(1);
            var client2 = new PinionCore.Remote.Gateway.Registrys.Client(2);
            var dispose1 = client1.Agent.Connect(service);
            var dispose2 = client2.Agent.Connect(service);

            using var worker1 = new AgentWorker(client1.Agent);


            using var worker2 = new AgentWorker(client2.Agent);
            


            TestContext.Progress.WriteLine("Awaiting server allocator for group 1...");
            var serverRequire1Obs = from s in server.LinesNotifier.SupplyEvent()
                                    where s.Group == 1
                                    select s;

            var alloc1 = await serverRequire1Obs.FirstAsync();
            TestContext.Progress.WriteLine("Allocator 1 received.");
            var serverStream1 = alloc1.Alloc();
            TestContext.Progress.WriteLine($"Server streams count: {(alloc1 as PinionCore.Remote.Gateway.Registrys.LineAllocator)?.StreamsNotifier.Base}");
            TestContext.Progress.WriteLine("Server stream 1 allocated.");

            var serverRequire2Obs = from s in server.LinesNotifier.SupplyEvent()
                                    where s.Group == 2
                                    select s;
            var alloc2 = await serverRequire2Obs.FirstAsync();
            var serverStream2 = alloc2.Alloc();


            var client1ResponseObs = from s in client1.Notifier.Base.SupplyEvent()
                                     select s;
            TestContext.Progress.WriteLine("Awaiting client stream 1...");
            var clientStream1 = await client1ResponseObs.FirstAsync();
            TestContext.Progress.WriteLine("Client stream 1 supplied.");



            var client2ResponseObs = from s in client2.Notifier.Base.SupplyEvent()
                                     select s;
            TestContext.Progress.WriteLine("Awaiting client stream 2...");
            var clientStream2 = await client2ResponseObs.FirstAsync();
            TestContext.Progress.WriteLine("Client stream 2 supplied.");


            var sendData1 = new byte[] { 1, 2, 3, 4, 5 };
            var count1 = await clientStream1.Send(sendData1, 0, sendData1.Length);
            Assert.AreEqual(sendData1.Length, count1);

            var receiveBuffer1 = new byte[5];
            var count2 = await serverStream1.Receive(receiveBuffer1, 0, receiveBuffer1.Length);
            Assert.AreEqual(sendData1.Length, count2);
            // check data integrity
            Assert.AreEqual(sendData1, receiveBuffer1);

            var sendData2 = new byte[] { 6, 7, 8, 9, 10 };
            var count3 = await clientStream2.Send(sendData2, 0, sendData2.Length);
            Assert.AreEqual(sendData2.Length, count3);
            var receiveBuffer2 = new byte[5];
            var count4 = await serverStream2.Receive(receiveBuffer2, 0, receiveBuffer2.Length);
            Assert.AreEqual(sendData2.Length, count4);
            // check data integrity
            Assert.AreEqual(sendData2, receiveBuffer2);

        }
    }
}


