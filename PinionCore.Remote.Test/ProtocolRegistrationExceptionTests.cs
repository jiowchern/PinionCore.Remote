using System;
using System.Collections.Concurrent;
using System.Reflection;
using NSubstitute;

namespace PinionCore.Remote.Tests
{
    public class ProtocolRegistrationExceptionTests
    {
        interface IUnregistered
        {
        }

        class Unregistered : IUnregistered
        {
        }

        private MemberMap _CreateEmptyMemberMap()
        {
            return new MemberMap(
                new System.Collections.Generic.Dictionary<int, MethodInfo>(),
                new System.Collections.Generic.Dictionary<int, EventInfo>(),
                new PropertyInfo[] { },
                new System.Tuple<System.Type, System.Func<PinionCore.Remote.IProvider>>[] { });
        }

        [NUnit.Framework.Test]
        public void BindUnregisteredInterfaceThrowsTest()
        {
            IProtocol protocol = NSubstitute.Substitute.For<IProtocol>();
            protocol.GetMemberMap().Returns(_CreateEmptyMemberMap());

            var handler = new SoulBindHandler(
                new IdLandlord(),
                NSubstitute.Substitute.For<IResponseQueue>(),
                protocol,
                new ConcurrentDictionary<long, SoulProxy>(),
                NSubstitute.Substitute.For<ISerializable>(),
                NSubstitute.Substitute.For<IInternalSerializable>(),
                new EventProvider(new IEventProxyCreator[] { }));

            IUnregistered soul = new Unregistered();
            Exceptions.UnregisteredProtocolInterfaceException exception = NUnit.Framework.Assert.Throws<Exceptions.UnregisteredProtocolInterfaceException>(() => handler.Bind(soul));

            NUnit.Framework.Assert.AreEqual(typeof(IUnregistered), exception.SoulType);
            NUnit.Framework.Assert.IsTrue(exception.Message.Contains(typeof(IUnregistered).FullName));
            NUnit.Framework.Assert.IsTrue(exception.Message.Contains("Protocolable"));
        }

        [NUnit.Framework.Test]
        public void LoadSoulUnknownTypeIdThrowsTest()
        {
            IProtocol protocol = NSubstitute.Substitute.For<IProtocol>();
            protocol.VersionCode.Returns(new byte[] { 1 });
            protocol.GetMemberMap().Returns(_CreateEmptyMemberMap());
            protocol.GetInterfaceProvider().Returns(new InterfaceProvider(new System.Collections.Generic.Dictionary<Type, Type>()));

            ISerializable serializer = NSubstitute.Substitute.For<ISerializable>();
            var handler = new ProviderHelper.GhostsHandler(
                protocol,
                serializer,
                NSubstitute.Substitute.For<IInternalSerializable>(),
                new ProviderHelper.GhostsOwner(protocol),
                new ProviderHelper.GhostsReturnValueHandler(serializer));

            Exceptions.UnknownProtocolTypeIdException exception = NUnit.Framework.Assert.Throws<Exceptions.UnknownProtocolTypeIdException>(() => handler.LoadSoul(42, 1, false));

            NUnit.Framework.Assert.AreEqual(42, exception.TypeId);
            NUnit.Framework.Assert.IsTrue(exception.Message.Contains("42"));
        }
    }
}
