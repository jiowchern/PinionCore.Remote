﻿
using Regulus.Serialization;
using System;

namespace Regulus.Remote.Tests
{

    public class PackageReleaseTests
    {
        [NUnit.Framework.Test]
        public void ToBufferTest1()
        {
            Guid id = Guid.NewGuid();
            TestPackageData package1 = new TestPackageData();

            var ser = new Regulus.Serialization.Serializer(new DescriberBuilder(typeof(Guid), typeof(TestPackageData)).Describers);
            package1.Id = id;

            
            byte[] buffer = ser.ObjectToBuffer(package1);

            
            TestPackageData package2 = ser.BufferToObject(buffer) as TestPackageData;

            NUnit.Framework.Assert.AreEqual(id, package2.Id);
        }

        [NUnit.Framework.Test]
        public void ToBufferTest2()
        {

            int p1 = 0;
            string p2 = "234";
            Guid p3 = Guid.NewGuid();
            TestPackageBuffer package1 = new TestPackageBuffer();
            var ser = new Regulus.Serialization.Serializer(new DescriberBuilder(typeof(int), typeof(string), typeof(char[]), typeof(byte), typeof(byte[]), typeof(byte[][]), typeof(char), typeof(Guid), typeof(TestPackageBuffer)).Describers);


            package1.Datas = new[] { ser.ObjectToBuffer(p1), ser.ObjectToBuffer(p2), ser.ObjectToBuffer(p3) };

            //byte[] buffer = package1.ToBuffer(ser);
            byte[] buffer = ser.ObjectToBuffer(package1);

            //TestPackageBuffer package2 = buffer.ToPackageData<TestPackageBuffer>(ser);
            TestPackageBuffer package2 = ser.BufferToObject(buffer) as TestPackageBuffer;


            NUnit.Framework.Assert.AreEqual(p1, ser.BufferToObject(package2.Datas[0]));
            NUnit.Framework.Assert.AreEqual(p2, ser.BufferToObject(package2.Datas[1]));
            NUnit.Framework.Assert.AreEqual(p3, ser.BufferToObject(package2.Datas[2]));
        }


        [NUnit.Framework.Test]
        public void ToPackageRequestTest()
        {

            DescriberBuilder builder = new Regulus.Serialization.DescriberBuilder(
                            typeof(System.Int32),
                            typeof(System.Char),
                            typeof(System.Char[]),
                            typeof(System.String),
                            typeof(System.Boolean),
                            typeof(Regulus.Remote.RequestPackage),
                            typeof(System.Byte[]),
                            typeof(System.Byte),
                            typeof(Regulus.Remote.ClientToServerOpCode),
                            typeof(Regulus.Remote.ResponsePackage),
                            typeof(Regulus.Remote.ServerToClientOpCode),
                            typeof(System.Guid),
                            typeof(Regulus.Remote.PackageInvokeEvent),
                            typeof(System.Byte[][]),
                            typeof(Regulus.Remote.PackageErrorMethod),
                            typeof(Regulus.Remote.PackageReturnValue),
                            typeof(Regulus.Remote.PackageLoadSoulCompile),
                            typeof(Regulus.Remote.PackageLoadSoul),
                            typeof(Regulus.Remote.PackageUnloadSoul),
                            typeof(Regulus.Remote.PackageCallMethod),
                            typeof(Regulus.Remote.PackageRelease));
            var ser = new Regulus.Serialization.Serializer(builder.Describers);
            RequestPackage response = new RequestPackage();
            response.Code = ClientToServerOpCode.Ping;
            response.Data = new byte[] { 0, 1, 2, 3, 4, 5 };

            byte[] bufferResponse = ser.ObjectToBuffer(response);
            RequestPackage result = ser.BufferToObject(bufferResponse) as RequestPackage;
            NUnit.Framework.Assert.AreEqual(ClientToServerOpCode.Ping, result.Code);
            NUnit.Framework.Assert.AreEqual(3, result.Data[3]);
        }






        [NUnit.Framework.Test]
        public void ToBufferTest3()
        {



            TestPackageBuffer package1 = new TestPackageBuffer();

            var ser = new Regulus.Serialization.Serializer(new DescriberBuilder(typeof(int), typeof(string), typeof(char[]), typeof(byte), typeof(byte[]), typeof(byte[][]), typeof(char), typeof(Guid), typeof(TestPackageBuffer)).Describers);

            package1.Datas = new byte[0][];

            //byte[] buffer = package1.ToBuffer(ser);
            byte[] buffer = ser.ObjectToBuffer(package1);

            TestPackageBuffer package2 = ser.BufferToObject(buffer) as TestPackageBuffer;


            NUnit.Framework.Assert.AreEqual(0, package2.Datas.Length);

        }
    }
    [Serializable]

    public class TestPackageData : TPackageData<TestPackageData>
    {

        public Guid Id;
    }
    [Serializable]

    public class TestPackageBuffer : TPackageData<TestPackageBuffer>
    {

        public TestPackageBuffer()
        {
            Datas = new byte[0][];
        }

        public byte[][] Datas;
    }



}