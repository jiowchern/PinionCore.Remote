using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using PinionCore.Remote.Reactive;
using PinionCore.Remote.Tools.Protocol.Sources.TestCommon.MultipleNotices;

namespace PinionCore.Remote.Tools.Protocol.Sources.TestCommon.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {

        }
        [Test]
        public void CreateIdentifyProtocol()
        {
            IProtocol protocol = PinionCore.Remote.Tools.Protocol.Sources.IdentifyTestCommon.ProtocolProvider.CreateCase1();
            var i1 = protocol.GetMemberMap().GetInterface(typeof(PinionCore.Remote.Tools.Protocol.Sources.IdentifyTestCommon.IInterface1)); ;
            var i2 = protocol.GetMemberMap().GetInterface(typeof(PinionCore.Remote.Tools.Protocol.Sources.IdentifyTestCommon.IInterface2)); ;

            NUnit.Framework.Assert.Zero(i2);
            NUnit.Framework.Assert.NotZero(i1);

        }
        [Test]
        public void CreateProtocolTest1()
        {

            IProtocol protocol = PinionCore.Remote.Tools.Protocol.Sources.TestCommon.ProtocolProvider.CreateCase1();
            NUnit.Framework.Assert.IsNotNull(protocol);
        }

        [Test]
        public void CreateProtocolTest2()
        {
            IProtocol protocol = PinionCore.Remote.Tools.Protocol.Sources.TestCommon.ProtocolProvider.CreateCase2();
            NUnit.Framework.Assert.IsNotNull(protocol);
        }

        [Test, Timeout(Timeout)]
        public void CreateProtocolSerializeTypesTest()
        {
            IProtocol protocol = PinionCore.Remote.Tools.Protocol.Sources.TestCommon.ProtocolProvider.CreateCase1();
            NUnit.Framework.Assert.IsTrue(protocol.SerializeTypes.Any(t => t == typeof(int)));

            NUnit.Framework.Assert.AreEqual(13, protocol.SerializeTypes.Length);


        }

        [Test, Timeout(Timeout)]
        public void CreateProtocolTest3()
        {
            IProtocol protocol = ProtocolProviderCase3.CreateCase3();
            NUnit.Framework.Assert.IsNotNull(protocol);
        }
        [Test, Timeout(Timeout)]
        public void NotifierSupplyAndUnsupplyTest()
        {
            var multipleNotices = new MultipleNotices.MultipleNotices();

            var env = new TestEnv<Entry<IMultipleNotices>, IMultipleNotices>(new Entry<IMultipleNotices>(multipleNotices));

            var n1 = new PinionCore.Remote.Tools.Protocol.Sources.TestCommon.Number(1);
            var n2 = new PinionCore.Remote.Tools.Protocol.Sources.TestCommon.Number(2);
            var n3 = new PinionCore.Remote.Tools.Protocol.Sources.TestCommon.Number(3);

            multipleNotices.Numbers1.Items.Add(n1);
            multipleNotices.Numbers1.Items.Add(n2);
            multipleNotices.Numbers1.Items.Add(n2);
            multipleNotices.Numbers1.Items.Add(n3);

            multipleNotices.Numbers2.Items.Add(n2);
            multipleNotices.Numbers2.Items.Add(n3);

            IObservable<int> supplyn1Obs = from mn in env.Queryable.QueryNotifier<IMultipleNotices>().SupplyEvent()
                                           from n in mn.Numbers1.Base.SupplyEvent()
                                           select n.Value.Value;

            IObservable<INumber> supplyn2Obs = from mn in env.Queryable.QueryNotifier<IMultipleNotices>().SupplyEvent()
                                               from n in mn.Numbers2.Base.SupplyEvent()
                                               select n;

            IObservable<int> unsupplyn1Obs = from mn in env.Queryable.QueryNotifier<IMultipleNotices>().SupplyEvent()
                                             from n in mn.Numbers1.Base.UnsupplyEvent()
                                             select n.Value.Value;

            IObservable<int> unsupplyn2Obs = from mn in env.Queryable.QueryNotifier<IMultipleNotices>().SupplyEvent()
                                             from n in mn.Numbers2.Base.UnsupplyEvent()
                                             select n.Value.Value;



            System.Collections.Generic.IList<int> num1s = supplyn1Obs.Buffer(4).FirstAsync().Wait();
            System.Collections.Generic.IList<INumber> num2s = supplyn2Obs.Buffer(2).FirstAsync().Wait();



            NUnit.Framework.Assert.AreEqual(1, num1s[0]);
            NUnit.Framework.Assert.AreEqual(2, num1s[1]);
            NUnit.Framework.Assert.AreEqual(2, num1s[2]);
            NUnit.Framework.Assert.AreEqual(3, num1s[3]);
            NUnit.Framework.Assert.AreEqual(2, num2s[0].Value.Value);
            NUnit.Framework.Assert.AreEqual(3, num2s[1].Value.Value);

            var removeNum1s = new System.Collections.Generic.List<int>();
            unsupplyn1Obs.Subscribe(removeNum1s.Add);
            unsupplyn2Obs.Subscribe(removeNum1s.Add);

            multipleNotices.Numbers1.Items.Remove(n2);
            multipleNotices.Numbers2.Items.Remove(n2);
            var c1 = multipleNotices.Numbers1.Items.Count;
            var c2 = multipleNotices.Numbers2.Items.Count;



            System.Threading.SpinWait.SpinUntil(() => removeNum1s.Count == 2, 60000);
            NUnit.Framework.Assert.AreEqual(2, removeNum1s[0]);
            NUnit.Framework.Assert.AreEqual(2, removeNum1s[1]);

            env.Dispose();
        }
        [Test, Timeout(Timeout)]
        public void NotifierSupplyTest()
        {


            var multipleNotices = new MultipleNotices.MultipleNotices();

            var env = new TestEnv<Entry<IMultipleNotices>, IMultipleNotices>(new Entry<IMultipleNotices>(multipleNotices));

            var n1 = new PinionCore.Remote.Tools.Protocol.Sources.TestCommon.Number(1);


            multipleNotices.Numbers1.Items.Add(n1);
            multipleNotices.Numbers1.Items.Add(n1);
            multipleNotices.Numbers2.Items.Add(n1);

            IObservable<int> supplyn1Obs = from mn in env.Queryable.QueryNotifier<IMultipleNotices>().SupplyEvent()
                                           from n in mn.Numbers1.Base.SupplyEvent()
                                           select n.Value.Value;

            IObservable<int> supplyn2Obs = from mn in env.Queryable.QueryNotifier<IMultipleNotices>().SupplyEvent()
                                           from n in mn.Numbers2.Base.SupplyEvent()
                                           select n.Value.Value;

            IObservable<int> unsupplyn1Obs = from mn in env.Queryable.QueryNotifier<IMultipleNotices>().SupplyEvent()
                                             from n in mn.Numbers1.Base.UnsupplyEvent()
                                             select n.Value.Value;

            IObservable<int> unsupplyn2Obs = from mn in env.Queryable.QueryNotifier<IMultipleNotices>().SupplyEvent()
                                             from n in mn.Numbers2.Base.UnsupplyEvent()
                                             select n.Value.Value;

            System.Collections.Generic.IList<int> num1s = supplyn1Obs.Buffer(2).FirstAsync().Wait();
            System.Collections.Generic.IList<int> num2s = supplyn2Obs.Buffer(1).FirstAsync().Wait();

            NUnit.Framework.Assert.AreEqual(1, num1s[0]);
            NUnit.Framework.Assert.AreEqual(1, num1s[1]);
            NUnit.Framework.Assert.AreEqual(1, num2s[0]);

            var removeNums = new System.Collections.Generic.List<int>();

            unsupplyn1Obs.Subscribe(removeNums.Add);
            unsupplyn2Obs.Subscribe(removeNums.Add);

            IObservable<int> count1Obs = from mn in env.Queryable.QueryNotifier<IMultipleNotices>().SupplyEvent()
                                         from count in mn.GetNumber1Count().RemoteValue()
                                         select count;

            IObservable<int> count2Obs = from mn in env.Queryable.QueryNotifier<IMultipleNotices>().SupplyEvent()
                                         from count in mn.GetNumber2Count().RemoteValue()
                                         select count;



            multipleNotices.Numbers2.Items.Remove(n1);
            multipleNotices.Numbers1.Items.Remove(n1);
            multipleNotices.Numbers1.Items.Remove(n1);

            var count1 = count1Obs.FirstAsync().Wait();
            var count2 = count2Obs.FirstAsync().Wait();

            NUnit.Framework.Assert.AreEqual(0, count1);
            NUnit.Framework.Assert.AreEqual(0, count2);


            System.Threading.SpinWait.SpinUntil(() => removeNums.Count == 3, 5000);
            NUnit.Framework.Assert.AreEqual(1, removeNums[0]);
            NUnit.Framework.Assert.AreEqual(1, removeNums[1]);
            NUnit.Framework.Assert.AreEqual(1, removeNums[2]);


            env.Dispose();
        }

        const int Timeout = 10000;
        [Test, Timeout(Timeout)]
        public void EventCustomDelegateTest()
        {
            var tester = new EventTester();


            var env = new TestEnv<Entry<IEventabe>, IEventabe>(new Entry<IEventabe>(tester));

            IObservable<IEventabe> eventerObs = from e in env.Queryable.QueryNotifier<IEventabe>().SupplyEvent()
                                                select e;

            IEventabe eventer = eventerObs.FirstAsync().Wait();

            CustomDelegate testAction = () => { };

            NUnit.Framework.Assert.Throws<PinionCore.Remote.Exceptions.NotSupportedException>(() => eventer.CustomDelegateEvent += testAction);



        }

        [Test, Timeout(Timeout)]
        public void EventRemoveTest()
        {
            var tester = new EventTester();


            var env = new TestEnv<Entry<IEventabe>, IEventabe>(new Entry<IEventabe>(tester));

            IObservable<IEventabe> eventerObs = from e in env.Queryable.QueryNotifier<IEventabe>().SupplyEvent()
                                                select e;

            IEventabe eventer = eventerObs.FirstAsync().Wait();

            System.Action actionEmpty = () => { };
            eventer.Event02 += actionEmpty;
            System.Threading.SpinWait.SpinUntil(() => tester.Event02AddCount == 1, 5000);

            eventer.Event02 -= actionEmpty;
            System.Threading.SpinWait.SpinUntil(() => tester.Event02RemoveCount == 1, 5000);



            env.Dispose();

            NUnit.Framework.Assert.AreEqual(1, tester.Event02AddCount);
            NUnit.Framework.Assert.AreEqual(1, tester.Event02RemoveCount);
        }

        [Test, Timeout(Timeout)]
        public void EventTest()
        {
            var tester = new EventTester();


            var env = new TestEnv<Entry<IEventabe>, IEventabe>(new Entry<IEventabe>(tester));



            IObservable<System.Reactive.Unit> event11Obs = from eventer in env.Queryable.QueryNotifier<IEventabe>().SupplyEvent()
                                                           from n in Reactive.Extensions.EventObservable((h) => eventer.Event1 += h, (h) => eventer.Event1 -= h)
                                                           select n;
            IObservable<System.Reactive.Unit> event12Obs = from eventer in env.Queryable.QueryNotifier<IEventabe>().SupplyEvent()
                                                           from n in Reactive.Extensions.EventObservable((h) => eventer.Event21 += h, (h) => eventer.Event21 -= h)
                                                           select n;

            IObservable<int> event21Obs = from eventer in env.Queryable.QueryNotifier<IEventabe>().SupplyEvent()
                                          from n in Reactive.Extensions.EventObservable<int>((h) => eventer.Event2 += h, (h) => eventer.Event2 -= h)
                                          select n;
            IObservable<int> event22Obs = from eventer in env.Queryable.QueryNotifier<IEventabe>().SupplyEvent()
                                          from n in Reactive.Extensions.EventObservable<int>(
                                              (h) => eventer.Event22 += h,
                                              (h) => eventer.Event22 -= h)
                                          select n;

            var vals = new System.Collections.Concurrent.ConcurrentQueue<int>();

            event11Obs.Subscribe((unit) => vals.Enqueue(1));
            event12Obs.Subscribe((unit) => vals.Enqueue(2));
            event21Obs.Subscribe(vals.Enqueue);
            event22Obs.Subscribe(vals.Enqueue);


            System.Console.WriteLine("wait EventTest tester.LisCount ...");
            System.Threading.SpinWait.SpinUntil(() => tester.LisCount == 4, 5000);
            

            tester.Invoke22(9);
            tester.Invoke21();
            tester.Invoke11();
            tester.Invoke12(8);


            System.Console.WriteLine("wait EventTest vals.Count ...");

            System.Threading.SpinWait.SpinUntil(() => vals.Count == 4, 5000);


            env.Dispose();

            var valsArray = vals.ToArray();
            var actualVals = new int[] { 9, 2, 1, 8 };
            NUnit.Framework.Assert.IsTrue(valsArray.Length == 4, "vals.Count != 4");
            NUnit.Framework.Assert.IsTrue(valsArray.Any(), "vals.Count != 4");
            NUnit.Framework.Assert.IsTrue(valsArray.SequenceEqual(actualVals), "vals.Count != 4, actualVals != valsArray");



        }


        [Test, Timeout(Timeout)]
        public void MethodNotSupportedTest()
        {
            var tester = new MethodTester();

            var env = new TestEnv<Entry<IMethodable>, IMethodable>(new Entry<IMethodable>(tester));
            IObservable<IMethodable> gpiObs = from g in env.Queryable.QueryNotifier<IMethodable>().SupplyEvent()
                                              select g;

            IMethodable gpi = gpiObs.FirstAsync().Wait();
            try
            {
                gpi.NotSupported();
            }
            catch (PinionCore.Remote.Exceptions.NotSupportedException sue)
            {

                NUnit.Framework.Assert.Pass();
                return;
            }

            NUnit.Framework.Assert.Fail();
        }

        [Test, Timeout(Timeout)]
        public void MethodTest()
        {


            var tester = new MethodTester();

            var env = new TestEnv<Entry<IMethodable>, IMethodable>(new Entry<IMethodable>(tester));
            var valuesObs = from gpi in env.Queryable.QueryNotifier<IMethodable>().SupplyEvent()
                            from v1 in gpi.GetValue1().RemoteValue()
                            from v2 in gpi.GetValue2().RemoteValue()
                            
                            from v0 in gpi.GetValue0(0, "", 0, 0, 0, Guid.Empty).RemoteValue()
                            select new { v1, v2, v0 };

            var values = valuesObs.FirstAsync().Wait();
            env.Dispose();

            Assert.AreEqual(1, values.v1);
            Assert.AreEqual(2, values.v2);
            Assert.AreEqual(0, values.v0[0]);
        }


        [Test, Timeout(Timeout)]
        public async Task MethodTestRevalue0_1()
        {
            var tester = new MethodTester();

            var env = new TestEnv<Entry<IMethodable>, IMethodable>(new Entry<IMethodable>(tester));
            var gpiObs = from gpi in env.Queryable.QueryNotifier<IMethodable>().SupplyEvent()
                            select gpi;

            var m = gpiObs.FirstAsync().Wait();

            var v = m.MethodNoValue(new TestStruct() { A = 1, B = "test" });

            await v;

            await System.Threading.Tasks.Task.Delay(100);
            env.Dispose();
            
        }
        
        //[Test , Timeout(1000*60)]
        public void MethodReturnTypeTest()
        {

            var tester = new MethodTester();

            var env = new TestEnv<Entry<IMethodable>, IMethodable>(new Entry<IMethodable>(tester));
            IObservable<IMethodable> methodObs = from gpi in env.Queryable.QueryNotifier<IMethodable>().SupplyEvent()
                                                 from v1 in gpi.GetValueSelf().RemoteValue()
                                                 select v1;
            System.Console.WriteLine("methodObs.FirstAsync().Wait()");
            IMethodable method = methodObs.FirstAsync().GetAwaiter().GetResult();

            IObservable<int> valueObs = from v1 in method.GetValue1().RemoteValue()
                                        select v1;
            System.Console.WriteLine("valueObs.FirstAsync().Wait()");
            var value = valueObs.FirstAsync().GetAwaiter().GetResult();

            method = null;

            System.Console.WriteLine("start gc collect");
            GC.Collect();
            System.Console.WriteLine("end gc collect");

            env.Dispose();
            Assert.AreEqual(1, value);
        }

        [Test, Timeout(Timeout)]
        public void MethodSayHelloTest()
        {


            var tester = new MethodTester();

            var env = new TestEnv<Entry<IMethodable>, IMethodable>(new Entry<IMethodable>(tester));
            IObservable<HelloReply> valuesObs = from gpi in env.Queryable.QueryNotifier<IMethodable>().SupplyEvent()
                                                from response in gpi.SayHello(new HelloRequest() { Name = "jc" }).RemoteValue()
                                                select response;

            HelloReply values = valuesObs.FirstAsync().Wait();
            env.Dispose();

            Assert.AreEqual("jc", values.Message);

        }

        [Test, Timeout(Timeout)]
        public void PropertyTest()
        {
            PinionCore.Utility.Singleton<PinionCore.Utility.Log>.Instance.RecordEvent += System.Console.WriteLine;
            var tester = new PropertyTester();
            var env = new TestEnv<Entry<IPropertyable>, IPropertyable>(new Entry<IPropertyable>(tester));

            IObservable<IPropertyable> gpiObs = from g in env.Queryable.QueryNotifier<IPropertyable>().SupplyEvent()
                                                select g;

            IPropertyable gpi = gpiObs.FirstAsync().Wait();

            Assert.AreEqual(1, gpi.Property1.Value);
            Assert.AreEqual(2, gpi.Property2.Value);

            tester.Property1.Value++;
            tester.Property2.Value++;

            System.Threading.SpinWait.SpinUntil(() =>
            {

                return gpi.Property1.Value == 2 && gpi.Property2.Value == 3;
            }, 60000);

            Assert.AreEqual(2, gpi.Property1.Value);
            Assert.AreEqual(3, gpi.Property2.Value);

            env.Dispose();


        }


    }
}
