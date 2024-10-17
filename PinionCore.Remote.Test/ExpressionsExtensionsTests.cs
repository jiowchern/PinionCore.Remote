using PinionCore.Remote.Extensions;
using NSubstitute;
namespace RemotingTest
{
    public class ExpressionsExtensionsTests
    {
        [NUnit.Framework.Test]
        public void Test()
        {
            var accesser = NSubstitute.Substitute.For<PinionCore.Remote.IObjectAccessible>();

            System.Linq.Expressions.Expression<PinionCore.Remote.GetObjectAccesserMethod> exp = (a) => a.Add;

            exp.Execute().Invoke(accesser, new object[] { accesser});
            

            accesser.Received().Add(accesser);
        }
    }
}
