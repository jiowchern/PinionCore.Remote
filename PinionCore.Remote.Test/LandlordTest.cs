using NUnit.Framework;

namespace PinionCore.Remote.Tests
{
    public class LandlordTest
    {
        [NUnit.Framework.Test]
        public void LongTest()
        {
            // id 不回收:歸還後仍遞增發新號,避免遲到請求錯投到重用 id 的新對象
            var landlord = new Landlord<long>(new LongProvider());
            var l1 = landlord.Rent();
            var l2 = landlord.Rent();
            landlord.Return(l2);
            var l3 = landlord.Rent();
            Assert.AreEqual(1, l1);
            Assert.AreEqual(2, l2);
            Assert.AreEqual(3, l3);
        }
    }
}
