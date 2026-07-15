namespace PinionCore.Remote.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Reflection;
    using NSubstitute;
    using NUnit.Framework;
    using PinionCore.Remote.Packages;

    public interface ITraceTestSpirit
    {
        PinionCore.Remote.Value<int> GetNumber();
    }

    public class SoulMethodHandlerErrorTests
    {
        [NUnit.Framework.Test]
        public void SoulNotFoundErrorPackageContainsMethodNameTest()
        {
            MethodInfo methodInfo = typeof(ITraceTestSpirit).GetMethod(nameof(ITraceTestSpirit.GetNumber));
            var memberMap = new MemberMap(
                new Dictionary<int, MethodInfo> { { 1, methodInfo } },
                new Dictionary<int, EventInfo>(),
                new PropertyInfo[0],
                new Tuple<Type, Func<IProvider>>[0]);

            IProtocol protocol = NSubstitute.Substitute.For<IProtocol>();
            protocol.GetMemberMap().Returns(memberMap);

            IRequestQueue peer = NSubstitute.Substitute.For<IRequestQueue>();
            IResponseQueue queue = NSubstitute.Substitute.For<IResponseQueue>();
            IInternalSerializable internalSerializer = new PinionCore.Remote.InternalSerializer();

            var responses = new List<Tuple<ServerToClientOpCode, PinionCore.Memorys.Buffer>>();
            queue.When(q => q.Push(NSubstitute.Arg.Any<ServerToClientOpCode>(), NSubstitute.Arg.Any<PinionCore.Memorys.Buffer>()))
                 .Do(ci => responses.Add(new Tuple<ServerToClientOpCode, PinionCore.Memorys.Buffer>(ci.ArgAt<ServerToClientOpCode>(0), ci.ArgAt<PinionCore.Memorys.Buffer>(1))));

            var souls = new ConcurrentDictionary<long, SoulProxy>();
            var bindHandler = new SoulBindHandler(
                new IdLandlord(),
                queue,
                protocol,
                souls,
                NSubstitute.Substitute.For<ISerializable>(),
                internalSerializer,
                new EventProvider(new IEventProxyCreator[0]));

            var handler = new SoulMethodHandler(
                peer,
                queue,
                protocol,
                souls,
                NSubstitute.Substitute.For<ISerializable>(),
                internalSerializer,
                bindHandler);

            peer.InvokeMethodEvent += NSubstitute.Raise.Event<InvokeMethodCallback>(999L, 1, 5L, new byte[0][]);

            NUnit.Framework.Assert.AreEqual(1, responses.Count);
            NUnit.Framework.Assert.AreEqual(ServerToClientOpCode.ErrorMethod, responses[0].Item1);
            var package = (PackageErrorMethod)internalSerializer.Deserialize(responses[0].Item2);
            NUnit.Framework.Assert.AreEqual($"{nameof(ITraceTestSpirit)}.{nameof(ITraceTestSpirit.GetNumber)}", package.Method);
            NUnit.Framework.Assert.AreEqual(5L, package.ReturnTarget);
            StringAssert.Contains("entity_id:999", package.Message);
            StringAssert.Contains("return_id:5", package.Message);

            handler.Dispose();
        }

        [NUnit.Framework.Test]
        public void SoulNotFoundErrorPackageUnknownMethodTest()
        {
            var memberMap = new MemberMap(
                new Dictionary<int, MethodInfo>(),
                new Dictionary<int, EventInfo>(),
                new PropertyInfo[0],
                new Tuple<Type, Func<IProvider>>[0]);

            IProtocol protocol = NSubstitute.Substitute.For<IProtocol>();
            protocol.GetMemberMap().Returns(memberMap);

            IRequestQueue peer = NSubstitute.Substitute.For<IRequestQueue>();
            IResponseQueue queue = NSubstitute.Substitute.For<IResponseQueue>();
            IInternalSerializable internalSerializer = new PinionCore.Remote.InternalSerializer();

            var responses = new List<Tuple<ServerToClientOpCode, PinionCore.Memorys.Buffer>>();
            queue.When(q => q.Push(NSubstitute.Arg.Any<ServerToClientOpCode>(), NSubstitute.Arg.Any<PinionCore.Memorys.Buffer>()))
                 .Do(ci => responses.Add(new Tuple<ServerToClientOpCode, PinionCore.Memorys.Buffer>(ci.ArgAt<ServerToClientOpCode>(0), ci.ArgAt<PinionCore.Memorys.Buffer>(1))));

            var souls = new ConcurrentDictionary<long, SoulProxy>();
            var bindHandler = new SoulBindHandler(
                new IdLandlord(),
                queue,
                protocol,
                souls,
                NSubstitute.Substitute.For<ISerializable>(),
                internalSerializer,
                new EventProvider(new IEventProxyCreator[0]));

            var handler = new SoulMethodHandler(
                peer,
                queue,
                protocol,
                souls,
                NSubstitute.Substitute.For<ISerializable>(),
                internalSerializer,
                bindHandler);

            peer.InvokeMethodEvent += NSubstitute.Raise.Event<InvokeMethodCallback>(999L, 42, 5L, new byte[0][]);

            NUnit.Framework.Assert.AreEqual(1, responses.Count);
            var package = (PackageErrorMethod)internalSerializer.Deserialize(responses[0].Item2);
            // 反查不到方法時退回 method_id 顯示
            NUnit.Framework.Assert.AreEqual("method_id:42", package.Method);

            handler.Dispose();
        }
    }
}
