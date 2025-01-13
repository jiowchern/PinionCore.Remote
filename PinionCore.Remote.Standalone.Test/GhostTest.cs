using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using NUnit.Framework;
using PinionCore.Memorys;
using PinionCore.Network;

namespace PinionCore.Remote.Standalone.Test
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

    public class ProtocolHelper
    {
        public static IProtocol CreateProtocol()
        {
            IProtocol protocol = NSubstitute.Substitute.For<IProtocol>();
            var types = new System.Collections.Generic.Dictionary<System.Type, System.Type>();
            types.Add(typeof(IGpiA), typeof(GhostIGpiA));
            var interfaceProvider = new InterfaceProvider(types);
            protocol.GetInterfaceProvider().Returns(interfaceProvider);

            System.Func<IProvider> gpiaProviderProvider = () => new TProvider<IGpiA>();
            var typeProviderProvider = new System.Tuple<System.Type, System.Func<IProvider>>[] { new System.Tuple<System.Type, System.Func<IProvider>>(typeof(IGpiA), gpiaProviderProvider) };
            protocol.GetMemberMap().Returns(new MemberMap(new System.Reflection.MethodInfo[0].ToDictionary(e=>0), new Dictionary<int, System.Reflection.EventInfo> { }, new System.Reflection.PropertyInfo[0], typeProviderProvider));
            return protocol;
        }
    }
    public class GhostTest
    {
        //[Test()]    
        public void CommunicationDevicePushTestMutli()
        {
            System.Collections.Generic.IEnumerable<System.Threading.Tasks.Task> tasks = from _ in System.Linq.Enumerable.Range(0, 10000000)
                                                                                        select CommunicationDevicePushTest();

            System.Threading.Tasks.Task.WhenAll(tasks);




        }
        [Test(/*Timeout = 5000*/)]
        public async System.Threading.Tasks.Task CommunicationDevicePushTest()
        {
            var sendBuf = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var recvBuf = new byte[11];

            var cd = new PinionCore.Remote.Standalone.Stream();
            var peer = cd as IStreamable;
            await cd.Push(sendBuf, 0, sendBuf.Length);


            var receiveCount1 = await peer.Receive(recvBuf, 0, 4);
            var receiveCount2 = await peer.Receive(recvBuf, 4, 5);
            var receiveCount3 = await peer.Receive(recvBuf, 9, 2);

            Assert.AreEqual(4, receiveCount1);
            Assert.AreEqual(5, receiveCount2);
            Assert.AreEqual(1, receiveCount3);
        }

        [Test(), Timeout(5000)]
        public async System.Threading.Tasks.Task CommunicationDevicePopTest()
        {
            var sendBuf = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var recvBuf = new byte[10];

            var cd = new PinionCore.Remote.Standalone.Stream();
            var peer = cd as IStreamable;

            IWaitableValue<int> result1 = peer.Send(sendBuf, 0, 4);
            var sendResult1 = await result1;

            IWaitableValue<int> result2 = peer.Send(sendBuf, 4, 6);
            var sendResult2 = await result2;


            IWaitableValue<int> streamTask1 = cd.Pop(recvBuf, 0, 3);
            var stream1 = await streamTask1;

            IWaitableValue<int> streamTask2 = cd.Pop(recvBuf, stream1, recvBuf.Length - stream1);
            var stream2 = await streamTask2;

            //var streamTask3 = cd.Pop(recvBuf, stream1 + stream2, recvBuf.Length - (stream1 + stream2));
            //int stream3 = await streamTask3;



            Assert.AreEqual(10, /*stream3 + */stream2 + stream1);
            Assert.AreEqual((byte)0, recvBuf[0]);
            Assert.AreEqual((byte)1, recvBuf[1]);
            Assert.AreEqual((byte)2, recvBuf[2]);
            Assert.AreEqual((byte)3, recvBuf[3]);
            Assert.AreEqual((byte)4, recvBuf[4]);
            Assert.AreEqual((byte)5, recvBuf[5]);
            Assert.AreEqual((byte)6, recvBuf[6]);
            Assert.AreEqual((byte)7, recvBuf[7]);
            Assert.AreEqual((byte)8, recvBuf[8]);
            Assert.AreEqual((byte)9, recvBuf[9]);

        }
        [Test(), Timeout(5000)]
        public async System.Threading.Tasks.Task CommunicationDeviceSerializerTest()
        {
            var serializer = new PinionCore.Remote.DynamicSerializer();
            IInternalSerializable internalSerializable = new InternalSerializer();
            var cd = new PinionCore.Remote.Standalone.Stream();
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
            var cd = new PinionCore.Remote.Standalone.Stream();
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
}
