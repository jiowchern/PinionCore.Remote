﻿
using System;
using System.Linq;
using PinionCore.Memorys;
using PinionCore.Serialization;


namespace PinionCore.Remote.Tests
{

    public class PackageReleaseTests
    {
        [NUnit.Framework.Test]
        public void ToBufferTest1()
        {
            var id = Guid.NewGuid();
            var package1 = new TestPackageData();

            var ser = new PinionCore.Serialization.Serializer(new DescriberBuilder(typeof(Guid), typeof(TestPackageData)).Describers);
            package1.Id = id;


            Memorys.Buffer buffer = ser.ObjectToBuffer(package1);


            var package2 = ser.BufferToObject(buffer) as TestPackageData;

            NUnit.Framework.Assert.AreEqual(id, package2.Id);
        }

        [NUnit.Framework.Test]
        public void ToBufferTest2()
        {

            var p1 = 0;
            var p2 = "234";
            var p3 = Guid.NewGuid();
            var package1 = new TestPackageBuffer();
            var ser = new PinionCore.Serialization.Serializer(new DescriberBuilder(typeof(int), typeof(string), typeof(char[]), typeof(byte), typeof(byte[]), typeof(byte[][]), typeof(char), typeof(Guid), typeof(TestPackageBuffer)).Describers);


            package1.Datas = new[] { ser.ObjectToBuffer(p1).ToArray(), ser.ObjectToBuffer(p2).ToArray(), ser.ObjectToBuffer(p3).ToArray() };

            //byte[] buffer = package1.ToBuffer(ser);
            Memorys.Buffer buffer = ser.ObjectToBuffer(package1);

            //TestPackageBuffer package2 = buffer.ToPackageData<TestPackageBuffer>(ser);
            var package2 = ser.BufferToObject(buffer) as TestPackageBuffer;


            NUnit.Framework.Assert.AreEqual(p1, ser.BufferToObject(package2.Datas[0].AsBuffer()));
            NUnit.Framework.Assert.AreEqual(p2, ser.BufferToObject(package2.Datas[1].AsBuffer()));
            NUnit.Framework.Assert.AreEqual(p3, ser.BufferToObject(package2.Datas[2].AsBuffer()));
        }


        [NUnit.Framework.Test]
        public void ToPackageRequestTest()
        {

            var builder = new PinionCore.Serialization.DescriberBuilder(
                            typeof(System.Int32),
                            typeof(System.Char),
                            typeof(System.Char[]),
                            typeof(System.String),
                            typeof(System.Boolean),
                            typeof(PinionCore.Remote.Packages.RequestPackage),
                            typeof(System.Byte[]),
                            typeof(System.Byte),
                            typeof(PinionCore.Remote.ClientToServerOpCode),
                            typeof(PinionCore.Remote.Packages.ResponsePackage),
                            typeof(PinionCore.Remote.ServerToClientOpCode),
                            typeof(System.Guid),
                            typeof(PinionCore.Remote.Packages.PackageInvokeEvent),
                            typeof(System.Byte[][]),
                            typeof(PinionCore.Remote.Packages.PackageErrorMethod),
                            typeof(PinionCore.Remote.Packages.PackageReturnValue),
                            typeof(PinionCore.Remote.Packages.PackageLoadSoulCompile),
                            typeof(PinionCore.Remote.Packages.PackageLoadSoul),
                            typeof(PinionCore.Remote.Packages.PackageUnloadSoul),
                            typeof(PinionCore.Remote.Packages.PackageCallMethod),
                            typeof(PinionCore.Remote.Packages.PackageRelease));
            var ser = new PinionCore.Serialization.Serializer(builder.Describers);
            var response = new PinionCore.Remote.Packages.RequestPackage();
            response.Code = ClientToServerOpCode.Ping;
            response.Data = new byte[] { 0, 1, 2, 3, 4, 5 };

            Memorys.Buffer bufferResponse = ser.ObjectToBuffer(response);
            var result = (PinionCore.Remote.Packages.RequestPackage)ser.BufferToObject(bufferResponse);
            NUnit.Framework.Assert.AreEqual(ClientToServerOpCode.Ping, result.Code);
            NUnit.Framework.Assert.AreEqual(3, result.Data[3]);
        }






        [NUnit.Framework.Test]
        public void ToBufferTest3()
        {



            var package1 = new TestPackageBuffer();

            var ser = new PinionCore.Serialization.Serializer(new DescriberBuilder(typeof(int), typeof(string), typeof(char[]), typeof(byte), typeof(byte[]), typeof(byte[][]), typeof(char), typeof(Guid), typeof(TestPackageBuffer)).Describers);

            package1.Datas = new byte[0][];

            //byte[] buffer = package1.ToBuffer(ser);
            Memorys.Buffer buffer = ser.ObjectToBuffer(package1);

            var package2 = ser.BufferToObject(buffer) as TestPackageBuffer;


            NUnit.Framework.Assert.AreEqual(0, package2.Datas.Length);

        }
    }
    [Serializable]

    public class TestPackageData
    {

        public Guid Id;
    }
    [Serializable]

    public class TestPackageBuffer
    {

        public TestPackageBuffer()
        {
            Datas = new byte[0][];
        }

        public byte[][] Datas;
    }



}
