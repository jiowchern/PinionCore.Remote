using NUnit.Framework;

namespace RemotingTest
{
    public class Property
    {
        [NUnit.Framework.Test]
        public void Equip()
        {
            var p1 = new PinionCore.Remote.Property<int>(1);
            var p2 = new PinionCore.Remote.Property<int>(1);

            var result = p1 == p2;
            Assert.True(result);
        }

        [NUnit.Framework.Test]
        public void PropertyUpdaterTest()
        {

            var p1 = new PinionCore.Remote.Property<int>(0);

            var updater = new PinionCore.Remote.PropertyUpdater(p1, 1);
            PinionCore.Remote.IPropertyIdValue idValue = updater;

            var objs = new System.Collections.Generic.Queue<object>();
            updater.ChnageEvent += (u, o) => objs.Enqueue(o);
            p1.Value = 1;
            p1.Value = 2;
            p1.Value = 3;

            Assert.True((int)objs.Dequeue() == 1);
            Assert.True((int)idValue.Instance == 1);
            updater.Reset();

            Assert.True((int)objs.Dequeue() == 3);
            Assert.True((int)idValue.Instance == 3);

            p1.Value = 4;
            p1.Value = 5;

            updater.Reset();
            Assert.True((int)objs.Dequeue() == 5);
            Assert.True((int)idValue.Instance == 5);
        }



    }
}

