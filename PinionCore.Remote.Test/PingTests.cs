
namespace PinionCore.Remote.Tests
{
    public class PingTests
    {
        [NUnit.Framework.Test]
        public void OnePingTest()
        {
            var ping = new PinionCore.Remote.Ping(1f);
            var count = 0;
            ping.TriggerEvent += () =>
            {
                count++;
            };

            System.Threading.Thread.Sleep(500);
            ping.GetSeconds();
            NUnit.Framework.Assert.AreEqual(0, count);


            System.Threading.Thread.Sleep(1000);
            ping.GetSeconds();
            NUnit.Framework.Assert.AreEqual(1, count);

            ping.Update();

            System.Threading.Thread.Sleep(500);
            ping.GetSeconds();
            NUnit.Framework.Assert.AreEqual(1, count);

            ping.Update();

            System.Threading.Thread.Sleep(3000);
            ping.GetSeconds();
            NUnit.Framework.Assert.AreEqual(2, count);

        }

        [NUnit.Framework.Test]
        public void TimeTest()
        {
            var ping = new PinionCore.Remote.Ping(1f);
            System.Threading.Thread.Sleep(1000);
            ping.Update();
            var sec1 = ping.GetSeconds();
            NUnit.Framework.Assert.LessOrEqual(1f, sec1);
            ping.Update();
            var sec2 = ping.GetSeconds();
            NUnit.Framework.Assert.LessOrEqual(sec2, 1f);



        }
    }
}
