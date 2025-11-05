using System.Linq;
using NSubstitute;
using NUnit.Framework;
using PinionCore.Memorys;
using PinionCore.Network;
using PinionCore.Remote;
using PinionCore.Remote.Extensions;
namespace RemotingTest
{
    public static class SerializerHelper
    {
        public static byte[] ServerToClient<T>(this PinionCore.Remote.IInternalSerializable serializer, ServerToClientOpCode opcode, T instance)
        {
            Buffer buf = serializer.Serialize(instance);
            var pkg = new PinionCore.Remote.Packages.ResponsePackage() { Code = opcode, Data = buf.ToArray() };

            buf = serializer.Serialize(pkg);
            var bytes = buf.ToArray();

            return bytes;
        }

    }
    public class StreamTests
    {
        [Test(), Timeout(5000)]
        public async System.Threading.Tasks.Task CommunicationDeviceSerializerTest()
        {
            var serializer = new PinionCore.Remote.DynamicSerializer();
            IInternalSerializable internalSerializable = new InternalSerializer();
            var cd = new PinionCore.Network.Stream();
            IStreamable peer = cd;
            var buf = internalSerializable.ServerToClient(ServerToClientOpCode.LoadSoul, new PinionCore.Remote.Packages.PackageLoadSoul() { EntityId = 1, ReturnType = false, TypeId = 1 });
            await cd.Push(buf, 0, buf.Length);

            var recvBuf = new byte[buf.Length];
            await peer.Receive(recvBuf, 0, recvBuf.Length);
            var responsePkg = (PinionCore.Remote.Packages.ResponsePackage)internalSerializable.Deserialize(recvBuf.AsBuffer());
            var lordsoulPkg = (PinionCore.Remote.Packages.PackageLoadSoul)internalSerializable.Deserialize(responsePkg.Data.AsBuffer());
            Assert.AreEqual(ServerToClientOpCode.LoadSoul, responsePkg.Code);
            Assert.AreEqual(1, lordsoulPkg.EntityId);
            Assert.False(lordsoulPkg.ReturnType);
            Assert.AreEqual(1, lordsoulPkg.TypeId);
        }

        [NUnit.Framework.Test(), NUnit.Framework.Timeout(5000)]

        public async System.Threading.Tasks.Task CommunicationDeviceSerializerBatchTest()
        {
            var serializer = new PinionCore.Remote.DynamicSerializer();
            IInternalSerializable internalSerializable = new InternalSerializer();
            var cd = new PinionCore.Network.Stream();
            IStreamable peer = cd;

            var buf = internalSerializable.ServerToClient(ServerToClientOpCode.LoadSoul, new PinionCore.Remote.Packages.PackageLoadSoul() { EntityId = 1, ReturnType = false, TypeId = 1 });

            await cd.Push(buf, 0, 1);
            await cd.Push(buf, 1, buf.Length - 1);

            var recvBuf = new byte[buf.Length];
            await peer.Receive(recvBuf, 0, recvBuf.Length);
            //await peer.Receive(recvBuf, 1, recvBuf.Length - 1);
            var responsePkg = (PinionCore.Remote.Packages.ResponsePackage)internalSerializable.Deserialize(recvBuf.AsBuffer());
            var lordsoulPkg = (PinionCore.Remote.Packages.PackageLoadSoul)internalSerializable.Deserialize(responsePkg.Data.AsBuffer());
            Assert.AreEqual(ServerToClientOpCode.LoadSoul, responsePkg.Code);
            Assert.AreEqual(1, lordsoulPkg.EntityId);
            Assert.False(lordsoulPkg.ReturnType);
            Assert.AreEqual(1, lordsoulPkg.TypeId);
        }


    }
    public class ExpressionsExtensionsTests
    {
        [NUnit.Framework.Test]
        public void Test()
        {
            PinionCore.Remote.IObjectAccessible accesser = NSubstitute.Substitute.For<PinionCore.Remote.IObjectAccessible>();

            System.Linq.Expressions.Expression<PinionCore.Remote.GetObjectAccesserMethod> exp = (a) => a.Add;

            exp.Execute().Invoke(accesser, new object[] { accesser });


            accesser.Received().Add(accesser);
        }
    }
}
