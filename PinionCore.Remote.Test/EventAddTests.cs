using System;
using NSubstitute;
using NUnit.Framework;
using PinionCore.Remote;

namespace RemotingTest
{


    public class SoulEventTests
    {
        public interface TestType
        {
            event System.Action TestEvent;
        }
        public class GhostTestType : TestType
        {
            public GhostTestType(PinionCore.Remote.GhostEventHandler handler)
            {
                _TestEvent = handler;
            }
            readonly PinionCore.Remote.GhostEventHandler _TestEvent;
            event Action TestType.TestEvent
            {
                add
                {
                    _TestEvent.Add(value);
                }

                remove
                {
                    _TestEvent.Remove(value);
                }
            }
        }
        [NUnit.Framework.Test]
        public void GhostEventInvokeTest()
        {
            var ghostEventHandler = new PinionCore.Remote.GhostEventHandler();

            var invokeEnable = false;
            var ghostTestType = new GhostTestType(ghostEventHandler) as TestType;
            ghostTestType.TestEvent += () =>
            {
                invokeEnable = true;
            };
            ghostEventHandler.Invoke(1);


            Assert.AreEqual(true, invokeEnable);

        }
        [NUnit.Framework.Test]
        public void SoulEventInvokeTest()
        {
            TestType obj = NSubstitute.Substitute.For<TestType>();
            var soul = new SoulProxy(0, 0, typeof(TestType), obj);

            var eventCatcher = "";
            InvokeEventCallabck callback = (entiry_id, event_id, handler_id, args) =>
            {
                eventCatcher = $"{entiry_id}-{event_id}-{handler_id}";
            };
            var closure = new GenericEventClosure(1, 1, 1, callback);
            System.Reflection.EventInfo info = typeof(TestType).GetEvent("TestEvent");
            soul.AddEvent(new SoulProxyEventHandler(obj, new System.Action(() => closure.Run()), info, 1));

            obj.TestEvent += Raise.Event<Action>();

            Assert.AreEqual("1-1-1", eventCatcher);

            soul.RemoveEvent(info, 1);
        }
    }
}

