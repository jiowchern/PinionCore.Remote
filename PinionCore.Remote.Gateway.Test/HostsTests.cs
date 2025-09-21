
using NUnit.Framework;
using PinionCore.Remote.Gateway.Hosts;
using PinionCore.Remote.Gateway.Protocols;
using PinionCore.Remote.Reactive;
using System.Reactive.Linq;
using System.Linq;
using System;


namespace PinionCore.Remote.Gateway.Tests
{
    public class HostsTests
    {
        [NUnit.Framework.Test, Timeout(10000)]
        public async System.Threading.Tasks.Task UserSessionSetAndLeaveTest()
        {
            var listener1 = new PinionCore.Remote.Gateway.Servers.Listener();
            PinionCore.Remote.Gateway.Protocols.IUserService service1 = listener1;
            

            var listener2 = new PinionCore.Remote.Gateway.Servers.Listener();
            PinionCore.Remote.Gateway.Protocols.IUserService service2 = listener2;

            var user1 = new PinionCore.Remote.Gateway.Hosts.ClientUser();
            ISession session1 = user1;
            IServiceSessionOwner owner1 = user1;

            var userSetObs1 = from userId in service1.Join().RemoteValue()
                       from serviceUser in service1.UserNotifier.Base.SupplyEvent()
                       where serviceUser.Id == userId
                       select new { userId , Result = session1.Set(1, serviceUser) };

            var setResult1 = await userSetObs1.FirstAsync();
            Assert.IsTrue(setResult1.Result);

            var userSetObs2 = from userId in service2.Join().RemoteValue()
                           from serviceUser in service2.UserNotifier.Base.SupplyEvent()
                           where serviceUser.Id == userId
                           select new { userId , Result = session1.Set(1, serviceUser) } ;
            var setResult2 = await userSetObs2.FirstAsync();
            Assert.IsFalse(setResult2.Result);


            var service2LeaveObs = from serviceUser in service2.UserNotifier.Base.UnsupplyEvent()
                               where serviceUser.Id == setResult2.userId
                                select service2.UserNotifier.Collection.Count;

            var count2 = new int?();
            service2LeaveObs.Subscribe(c => count2 = c);
            var service2LeaveResult = await service2.Leave(setResult2.userId);
            Assert.AreEqual(ReturnCode.Success, service2LeaveResult);
            while (!count2.HasValue)
                await System.Threading.Tasks.Task.Delay(10);

            Assert.AreEqual(0 , count2.Value);

            var service1LeaveObs = from serviceUser in service1.UserNotifier.Base.UnsupplyEvent()
                                   where serviceUser.Id == setResult1.userId
                                   select service2.UserNotifier.Collection.Count;

            var count1 = new int?();
            service1LeaveObs.Subscribe(c => count1 = c);
            var service1LeaveResult = await service1.Leave(setResult1.userId);
            Assert.AreEqual(ReturnCode.Success, service1LeaveResult);
            while (!count1.HasValue)
                await System.Threading.Tasks.Task.Delay(10);

            Assert.AreEqual(0, count1.Value);
        }
        
        [NUnit.Framework.Test, Timeout(10000)]
        public async System.Threading.Tasks.Task Test()
        {
           /* var route = new PinionCore.Remote.Gateway.Hosts.Router();
            var user = new PinionCore.Remote.Gateway.Hosts.ClientUser();

            var listener = new PinionCore.Remote.Gateway.Servers.Listener();
            PinionCore.Remote.Gateway.Protocols.IUserService service1 = listener;


            route.Join(user);

            var groups = user.GetGroups();

            Assert.AreEqual(0, groups.Length);

            route.Register(1, service1);

            Assert.AreEqual(1, groups.Length);
            Assert.AreEqual(1, groups[0]);


            route.Unregister(service1);

            groups = user.GetGroups();
            Assert.AreEqual(0, groups.Length);

            route.Leave(user);*/
        }
    }
}
