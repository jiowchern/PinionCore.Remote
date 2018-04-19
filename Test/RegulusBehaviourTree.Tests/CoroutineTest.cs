using System.Collections.Generic;
using NUnit.Framework;
using Regulus.BehaviourTree.Yield;

namespace Regulus.BehaviourTree.Tests
{

    class CoroutineTestObject
    {
        
        public IEnumerable<IInstructable> DirectBreak()
        {
            yield break;
        }

        public IEnumerable<IInstructable> DirectNull()
        {
            yield return null;
        }

        public IEnumerable<IInstructable> DirectCount3ToFailure()
        {
            
            yield return new Wait();
            
            yield return new Wait();
            
            yield return new Failure();

            yield return new Success();
        }

        public IEnumerable<IInstructable> UntilCount3ToFailure()
        {
            var count = 0;
            yield return new WaitUntil(() =>
            {                
                return ++count > 3;
            });

            
        }
    }

    public class CoroutineTest
    {
        [NUnit.Framework.Test()]
        public void TestDirectBreak()
        {
            var obj = new CoroutineTestObject();
            ITicker coroutine = new Regulus.BehaviourTree.Yield.Coroutine(() => obj.DirectBreak());
            var res1 = coroutine.Tick(0);
            Assert.AreEqual(TICKRESULT.SUCCESS, res1);            
        }

        [NUnit.Framework.Test()]
        public void TestDirectNull()
        {
            var obj = new CoroutineTestObject();
            ITicker coroutine = new Regulus.BehaviourTree.Yield.Coroutine(() => obj.DirectNull());
            var res1 = coroutine.Tick(0);
            Assert.AreEqual(TICKRESULT.SUCCESS, res1);
        }

        [NUnit.Framework.Test()]
        public void TestDirectCount3ToFailure()
        {
            var obj = new CoroutineTestObject();
            ITicker coroutine = new Regulus.BehaviourTree.Yield.Coroutine(() => obj.DirectCount3ToFailure());
            var res1 = coroutine.Tick(0);
            var res2 = coroutine.Tick(0);
            var res3 = coroutine.Tick(0);
            var res4 = coroutine.Tick(0);
            var res5 = coroutine.Tick(0);
            Assert.AreEqual(TICKRESULT.RUNNING, res1);
            Assert.AreEqual(TICKRESULT.RUNNING, res2);
            Assert.AreEqual(TICKRESULT.RUNNING, res3);
            Assert.AreEqual(TICKRESULT.FAILURE, res4);
            Assert.AreEqual(TICKRESULT.RUNNING, res5);
        }

        [NUnit.Framework.Test()]
        public void UntilCount3ToFailureToSuccess()
        {
            var obj = new CoroutineTestObject();
            ITicker coroutine = new Regulus.BehaviourTree.Yield.Coroutine(() => obj.UntilCount3ToFailure());
            var res1 = coroutine.Tick(0);
            var res2 = coroutine.Tick(0);
            var res3 = coroutine.Tick(0);
            var res4 = coroutine.Tick(0);
            var res5 = coroutine.Tick(0);
            var res6 = coroutine.Tick(0);
            Assert.AreEqual(TICKRESULT.RUNNING, res1);
            Assert.AreEqual(TICKRESULT.RUNNING, res2);
            Assert.AreEqual(TICKRESULT.RUNNING, res3);
            Assert.AreEqual(TICKRESULT.RUNNING, res4);
            Assert.AreEqual(TICKRESULT.RUNNING, res5);
            Assert.AreEqual(TICKRESULT.SUCCESS, res6);
        }


    }
}