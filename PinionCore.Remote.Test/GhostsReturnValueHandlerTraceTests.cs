namespace PinionCore.Remote.Tests
{
    using NUnit.Framework;
    using PinionCore.Remote.ProviderHelper;

    public class GhostsReturnValueHandlerTraceTests
    {
        [NUnit.Framework.Test]
        public void CaptureCallerStackOnErrorContainsCallSiteTest()
        {
            var handler = new GhostsReturnValueHandler(NSubstitute.Substitute.For<ISerializable>());
            handler.CaptureCallerStack = true;

            var value = new PinionCore.Remote.Value<int>();
            IValue pushed = value;
            var id = handler.PushReturnValue(pushed);

            handler.ErrorReturnValue(id, "ISpirit.Foo", "Soul not found entity_id:9");

            var error = value.GetError();
            StringAssert.StartsWith("ISpirit.Foo: Soul not found entity_id:9", error);
            StringAssert.Contains("--- RPC call site", error);
            // Push 發生在本測試方法的呼叫堆疊上,錯誤訊息應能指出呼叫點
            StringAssert.Contains(nameof(CaptureCallerStackOnErrorContainsCallSiteTest), error);
        }

        [NUnit.Framework.Test]
        public void CaptureCallerStackOffErrorHasNoCallSiteTest()
        {
            var handler = new GhostsReturnValueHandler(NSubstitute.Substitute.For<ISerializable>());

            var value = new PinionCore.Remote.Value<int>();
            IValue pushed = value;
            var id = handler.PushReturnValue(pushed);

            handler.ErrorReturnValue(id, "ISpirit.Foo", "Soul not found entity_id:9");

            NUnit.Framework.Assert.AreEqual("ISpirit.Foo: Soul not found entity_id:9", value.GetError());
        }

        [NUnit.Framework.Test]
        public void LateErrorAfterPopDoesNotThrowTest()
        {
            var handler = new GhostsReturnValueHandler(NSubstitute.Substitute.For<ISerializable>());
            handler.CaptureCallerStack = true;

            var value = new PinionCore.Remote.Value<int>();
            IValue pushed = value;
            var id = handler.PushReturnValue(pushed);

            // 先以成功路徑取走,再收到遲到的錯誤回應:不能毒死訊息迴圈
            IValue popped = handler.GetReturnValue(id);
            NUnit.Framework.Assert.AreSame(pushed, popped);
            NUnit.Framework.Assert.DoesNotThrow(() => handler.ErrorReturnValue(id, "ISpirit.Foo", "late error"));
            NUnit.Framework.Assert.IsNull(value.GetError());
        }
    }
}
