using System;
using System.Linq;
using System.Threading.Tasks;
using PinionCore.Remote;
using PinionCore.Serialization;


namespace PinionCore.Network.Tests
{
    public class PackageTest
    {
        struct TestStruct
        {
            public int A;
        }
        [NUnit.Framework.Test]
        public async System.Threading.Tasks.Task Test1()
        {
            var serializer = new PinionCore.Serialization.Serializer(new DescriberBuilder(typeof(int), typeof(string), typeof(char[]), typeof(byte), typeof(byte[]), typeof(byte[][]), typeof(char), typeof(Guid), typeof(TestStruct)).Describers, PinionCore.Memorys.PoolProvider.DirectShared);

            var sendStream = new Stream();

            var readStream = new PinionCore.Network.ReverseStream(sendStream);

            var tu = new ThreadUpdater(() =>
            {
                //sendStream.Receive.Digestion();
                // sendStream.Send.Digestion();
            });
            tu.Start();

            var sender = new PinionCore.Network.PackageSender(sendStream, PinionCore.Memorys.PoolProvider.Shared);
            var testStruct = new TestStruct();
            testStruct.A = -1;

            Memorys.Buffer testStructBuffer = serializer.ObjectToBuffer(testStruct);

            sender.Push(testStructBuffer);


            var reader = new PinionCore.Network.PackageReader(readStream, PinionCore.Memorys.PoolProvider.Shared);



            System.Collections.Generic.List<Memorys.Buffer> buffers = reader.Read().GetAwaiter().GetResult();
            Memorys.Buffer buffer = buffers.Single();


            var readStruct = (TestStruct)serializer.BufferToObject(buffer);
            NUnit.Framework.Assert.AreEqual(testStruct.A, readStruct.A);

            tu.Stop();
        }

        [NUnit.Framework.Test]
        public async System.Threading.Tasks.Task Test2()
        {
            var serializer = new PinionCore.Serialization.Serializer(new DescriberBuilder(typeof(int), typeof(string), typeof(char[]), typeof(byte), typeof(byte[]), typeof(byte[][]), typeof(char), typeof(Guid), typeof(TestStruct)).Describers, PinionCore.Memorys.PoolProvider.DirectShared);

            var sendStream = new Stream();
            var readStream = new PinionCore.Network.ReverseStream(sendStream);


            var sender = new PinionCore.Network.PackageSender(sendStream, PinionCore.Memorys.PoolProvider.Shared);
            Memorys.Buffer testStructBuffer1 = serializer.ObjectToBuffer(null);
            sender.Push(testStructBuffer1);

            Memorys.Buffer testStructBuffer2 = serializer.ObjectToBuffer(1);
            sender.Push(testStructBuffer2);

            Memorys.Buffer testStructBuffer3 = serializer.ObjectToBuffer(2);
            sender.Push(testStructBuffer3);
            var testStruct = new TestStruct();
            testStruct.A = -1;
            Memorys.Buffer testStructBuffer4 = serializer.ObjectToBuffer(testStruct);
            sender.Push(testStructBuffer4);




            var reader = new PinionCore.Network.PackageReader(readStream, PinionCore.Memorys.PoolProvider.Shared);

            var buffers = new System.Collections.Generic.List<Memorys.Buffer>();

            while (buffers.Count < 4)
            {
                System.Collections.Generic.List<Memorys.Buffer> bufs = await reader.Read();
                if (bufs.Count == 0)
                    break;
                buffers.AddRange(bufs);
            }

            {
                Memorys.Buffer buffer = buffers.ElementAt(0);
                var readStruct = serializer.BufferToObject(buffer);
                NUnit.Framework.Assert.AreEqual(null, readStruct);
            }
            for (var i = 1; i < 3; i++)
            {
                Memorys.Buffer buffer = buffers.ElementAt(i);
                var readStruct = (int)serializer.BufferToObject(buffer);
                NUnit.Framework.Assert.AreEqual(i, readStruct);
            }


            {
                Memorys.Buffer buffer = buffers.ElementAt(3);
                var readStruct = (TestStruct)serializer.BufferToObject(buffer);
                NUnit.Framework.Assert.AreEqual(-1, readStruct.A);
            }






        }

        [NUnit.Framework.Test]
        public async System.Threading.Tasks.Task Test3()
        {
            var serializer = new PinionCore.Serialization.Serializer(new DescriberBuilder(typeof(int), typeof(string), typeof(char[]), typeof(byte), typeof(byte[]), typeof(byte[][]), typeof(char), typeof(Guid), typeof(TestStruct)).Describers, PinionCore.Memorys.PoolProvider.DirectShared);

            var sendStream = new Stream();
            var readStream = new PinionCore.Network.ReverseStream(sendStream);


            var sender = new PinionCore.Network.PackageSender(sendStream, PinionCore.Memorys.PoolProvider.Shared);

            Memorys.Buffer buff = PinionCore.Memorys.PoolProvider.DirectShared.Alloc(0);
            sender.Push(buff);

            Memorys.Buffer testStructBuffer2 = serializer.ObjectToBuffer(null);
            sender.Push(testStructBuffer2);

            Memorys.Buffer testStructBuffer3 = serializer.ObjectToBuffer(null);
            sender.Push(testStructBuffer3);

            var reader = new PinionCore.Network.PackageReader(readStream, PinionCore.Memorys.PoolProvider.Shared);

            var buffers = new System.Collections.Generic.List<PinionCore.Memorys.Buffer>();
            while (buffers.Count < 2)
            {
                System.Collections.Generic.List<Memorys.Buffer> bufs = await reader.Read();
                if (bufs.Count == 0)
                    break;

                buffers.AddRange(bufs);
            }

            for (var i = 0; i < 2; i++)
            {
                Memorys.Buffer buffer = buffers.ElementAt(i);
                var readStruct = serializer.BufferToObject(buffer);
                NUnit.Framework.Assert.AreEqual(null, readStruct);
            }


        }
        [NUnit.Framework.Test]
        public async Task SendTest()
        {
            var stream = new PinionCore.Network.Stream();
            var sender = new PinionCore.Network.PackageSender(stream, PinionCore.Memorys.PoolProvider.Shared);

            var buffers = new System.Collections.Generic.List<PinionCore.Memorys.Buffer>();
            for (var i = 0; i < 1000; i++)
            {
                var buffer = PinionCore.Memorys.PoolProvider.Shared.Alloc(100);
                for (var j = 0; j < 100; j++)
                {
                    buffer.Bytes.Array[buffer.Bytes.Offset + j] = (byte)(j + i);
                    
                }
                buffers.Add(buffer);
                
            }

            foreach (var buffer in buffers)
            {
                sender.Push(buffer);
            }
            var reader = new PinionCore.Network.PackageReader(new PinionCore.Network.ReverseStream(stream), PinionCore.Memorys.PoolProvider.Shared);

            var readed = 0;
            while (readed < buffers.Count)
            {
                var readBuffers = await reader.Read();
                if (readBuffers.Count == 0)
                    break;
                foreach (var readBuffer in readBuffers)
                {
                    NUnit.Framework.Assert.AreEqual(100, readBuffer.Bytes.Count);
                    for (var i = 0; i < 100; i++)
                    {
                        NUnit.Framework.Assert.AreEqual((byte)(i + readed), readBuffer.Bytes.Array[readBuffer.Bytes.Offset + i]);
                    }
                    readed++;
                }
            }

            NUnit.Framework.Assert.AreEqual(buffers.Count, readed);
        }
    }
}
