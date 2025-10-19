using System;
using System.Net;
using System.Reactive.Linq;
using NUnit.Framework;
using PinionCore.Network;
using PinionCore.Remote.Gateway.Registrys;
using PinionCore.Remote.Gateway.Servers;
using PinionCore.Remote.Reactive;
using PinionCore.Remote.Soul;
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

            var worker1 = new AgentWorker(client1.Agent);
            worker1.Start();

            var worker2 = new AgentWorker(client2.Agent);
            worker2.Start();


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

            
            var client1ResponseObs = from s in client1.Notifier.Base.SupplyEvent()
                                     select s;
            var clientStream1 = await client1ResponseObs.FirstAsync();



            var client2ResponseObs = from s in client2.Notifier.Base.SupplyEvent()
                                     select s;
            var clientStream2 = await client2ResponseObs.FirstAsync();


            var sendData1 = new byte[] { 1, 2, 3, 4, 5 };
            var count1 = await clientStream1.Send(sendData1, 0, sendData1.Length);
            Assert.AreEqual(sendData1.Length, count1);

            var receiveBuffer1 = new byte[5];
            var count2 = await serverStream1.Receive(receiveBuffer1, 0, receiveBuffer1.Length);
            Assert.AreEqual(sendData1.Length, count2);

            var sendData2 = new byte[] { 6, 7, 8, 9, 10 };
            var count3 = await clientStream2.Send(sendData2, 0, sendData2.Length);
            Assert.AreEqual(sendData2.Length, count3);
            var receiveBuffer2 = new byte[5];
            var count4 = await serverStream2.Receive(receiveBuffer2, 0, receiveBuffer2.Length);
            Assert.AreEqual(sendData2.Length, count4);

            await worker1.StopAsync();
            await worker2.StopAsync();
            
            dispose2();
            dispose1();


        }
    }
}

