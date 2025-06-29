
namespace PinionCore.Remote.Tests
{
    using System.Reactive.Linq;
    using NUnit.Framework;
    using PinionCore.Remote.Reactive;
    public class ValueTests
    {

        [NUnit.Framework.Test]
        public async System.Threading.Tasks.Task ConstructorOnValueTest()
        {
            var val = new PinionCore.Remote.Value<int>(1);
            System.IObservable<int> vObs = from v in val.RemoteValue()
                                           select v;
            var result = await vObs.FirstAsync();
            NUnit.Framework.Assert.AreEqual(1, result);

        }
        [NUnit.Framework.Test]
        public async System.Threading.Tasks.Task SetOnValueTest()
        {
            var val = new PinionCore.Remote.Value<int>();
            System.IObservable<int> vObs = from v in val.RemoteValue()
                                           select v;

            val.SetValue(1);
            var result = await vObs.FirstAsync();
            NUnit.Framework.Assert.AreEqual(1, result);

        }

         [NUnit.Framework.Test]
         public async System.Threading.Tasks.Task ConstructorAwaitOnValueTest()
         {
             var val = await new PinionCore.Remote.Value<int>(1);

             NUnit.Framework.Assert.AreEqual(1, val);
         }

         [NUnit.Framework.Test]
         public async System.Threading.Tasks.Task SetAwaitOnValueTest()
         {
             var val = new PinionCore.Remote.Value<int>();
             val.SetValue(1);

             NUnit.Framework.Assert.AreEqual(1, await val);
         }

        [NUnit.Framework.Test , Timeout(1000)]
        public async System.Threading.Tasks.Task TestNoReturn()
        {
            var val = new PinionCore.Remote.Value(false) ;
            
            await val;
            
        }


    }
}
