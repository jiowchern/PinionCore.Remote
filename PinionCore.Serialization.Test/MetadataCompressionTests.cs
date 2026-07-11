using System;
using NUnit.Framework;

namespace PinionCore.Serialization.Tests
{
    public class MetadataCompressionTests
    {
        [Test]
        public void CallMethodLikePackageSizeTest()
        {
            var finder = new DescribersFinder(typeof(FakeCallMethod), typeof(long), typeof(int), typeof(byte[]), typeof(byte[][]), typeof(byte));
            var provider = new DescriberProvider(finder);
            var serializer = new Serializer(provider);

            var package = new FakeCallMethod
            {
                EntityId = 1,
                MethodId = 5,
                ReturnId = 2,
                MethodParams = new[] { new byte[] { 1, 2, 3 }, new byte[] { 4, 5, 6, 7 } }
            };

            var buffer = serializer.ObjectToBuffer(package);
            Console.WriteLine($"FakeCallMethod total size: {buffer.Count} bytes");

            // 新格式: typeid(1) + bitmask(1) + EntityId(1) + MethodId(1)
            //        + MethodParams[totalLen(1)+validLen(1)+ (len+bytes)*2 = 11] + ReturnId(1) = 16
            // 舊格式(count+index+每欄位/元素 type-id)約 28 bytes
            Assert.LessOrEqual(buffer.Count, 16, "field bitmask + omitted type-ids should shrink call packages");

            var result = (FakeCallMethod)serializer.BufferToObject(buffer);
            Assert.AreEqual(package.EntityId, result.EntityId);
            Assert.AreEqual(package.MethodId, result.MethodId);
            Assert.AreEqual(package.ReturnId, result.ReturnId);
            Assert.AreEqual(package.MethodParams.Length, result.MethodParams.Length);
            Assert.AreEqual(package.MethodParams[0], result.MethodParams[0]);
            Assert.AreEqual(package.MethodParams[1], result.MethodParams[1]);
        }

        [Test]
        public void DenseIntArraySizeTest()
        {
            var finder = new DescribersFinder(typeof(int[]), typeof(int));
            var provider = new DescriberProvider(finder);
            var serializer = new Serializer(provider);

            var array = new[] { 1, 2, 3, 4, 5 };
            var buffer = serializer.ObjectToBuffer(array);
            Console.WriteLine($"Dense int[5] size: {buffer.Count} bytes");

            // typeid(1) + totalLen(1) + validLen(1) + 5 個 varint 值 = 8
            Assert.LessOrEqual(buffer.Count, 8, "dense arrays should omit per-element index and type-id");

            var result = (int[])serializer.BufferToObject(buffer);
            Assert.AreEqual(array, result);
        }

        [Test]
        public void SparseIntArrayRoundTripTest()
        {
            var finder = new DescribersFinder(typeof(int[]), typeof(int));
            var provider = new DescriberProvider(finder);
            var serializer = new Serializer(provider);

            // 0 是 int 的 default,會被 sparse 編碼跳過,解碼端需正確還原
            var array = new[] { 7, 0, -3, 0, 0, 9 };
            var buffer = serializer.ObjectToBuffer(array);

            var result = (int[])serializer.BufferToObject(buffer);
            Assert.AreEqual(array, result);
        }

        [Test]
        public void ManyFieldsBitmaskRoundTripTest()
        {
            var finder = new DescribersFinder(typeof(NineFields), typeof(int), typeof(string), typeof(char), typeof(char[]));
            var provider = new DescriberProvider(finder);
            var serializer = new Serializer(provider);

            // 9 個欄位 → 2-byte bitmask,只設部分欄位驗證跨 byte 的位元運算
            var instance = new NineFields { F1 = 1, F5 = -5, F9 = "nine" };
            var buffer = serializer.ObjectToBuffer(instance);

            var result = (NineFields)serializer.BufferToObject(buffer);
            Assert.AreEqual(instance.F1, result.F1);
            Assert.AreEqual(0, result.F2);
            Assert.AreEqual(0, result.F3);
            Assert.AreEqual(0, result.F4);
            Assert.AreEqual(instance.F5, result.F5);
            Assert.AreEqual(0, result.F6);
            Assert.AreEqual(0, result.F7);
            Assert.AreEqual(0, result.F8);
            Assert.AreEqual(instance.F9, result.F9);
        }

        [Test]
        public void AllDefaultFieldsRoundTripTest()
        {
            var finder = new DescribersFinder(typeof(NineFields), typeof(int), typeof(string), typeof(char), typeof(char[]));
            var provider = new DescriberProvider(finder);
            var serializer = new Serializer(provider);

            var instance = new NineFields();
            var buffer = serializer.ObjectToBuffer(instance);
            Console.WriteLine($"All-default NineFields size: {buffer.Count} bytes");

            var result = (NineFields)serializer.BufferToObject(buffer);
            Assert.AreEqual(0, result.F1);
            Assert.AreEqual(null, result.F9);
        }

        [Test]
        public void PolymorphicFieldStillCarriesTypeIdTest()
        {
            var finder = new DescribersFinder(typeof(PolyOwner), typeof(PolyBase), typeof(PolyDerived), typeof(int));
            var provider = new DescriberProvider(finder);
            var serializer = new Serializer(provider);

            // 宣告型別 PolyBase 非 sealed → 仍寫 runtime type-id,子類欄位需完整還原
            var instance = new PolyOwner { Target = new PolyDerived { BaseValue = 3, DerivedValue = 4 } };
            var buffer = serializer.ObjectToBuffer(instance);

            var result = (PolyOwner)serializer.BufferToObject(buffer);
            PolyDerived derived = result.Target as PolyDerived;
            Assert.NotNull(derived);
            Assert.AreEqual(3, derived.BaseValue);
            Assert.AreEqual(4, derived.DerivedValue);
        }

        [Test]
        public void SealedClassFieldOmitsTypeIdTest()
        {
            var sealedFinder = new DescribersFinder(typeof(SealedOwner), typeof(SealedItem), typeof(int));
            var sealedSerializer = new Serializer(new DescriberProvider(sealedFinder));

            var openFinder = new DescribersFinder(typeof(OpenOwner), typeof(OpenItem), typeof(int));
            var openSerializer = new Serializer(new DescriberProvider(openFinder));

            var sealedBuffer = sealedSerializer.ObjectToBuffer(new SealedOwner { Item = new SealedItem { Value = 1 } });
            var openBuffer = openSerializer.ObjectToBuffer(new OpenOwner { Item = new OpenItem { Value = 1 } });
            Console.WriteLine($"sealed field: {sealedBuffer.Count} bytes, non-sealed field: {openBuffer.Count} bytes");

            Assert.Less(sealedBuffer.Count, openBuffer.Count, "sealed field type should not carry a runtime type-id");

            var result = (SealedOwner)sealedSerializer.BufferToObject(sealedBuffer);
            Assert.AreEqual(1, result.Item.Value);
        }

        public struct FakeCallMethod
        {
            public long EntityId;
            public int MethodId;
            public byte[][] MethodParams;
            public long ReturnId;
        }

        public struct NineFields
        {
            public int F1;
            public int F2;
            public int F3;
            public int F4;
            public int F5;
            public int F6;
            public int F7;
            public int F8;
            public string F9;
        }

        public class PolyBase
        {
            public int BaseValue;
        }

        public class PolyDerived : PolyBase
        {
            public int DerivedValue;
        }

        public class PolyOwner
        {
            public PolyBase Target;
        }

        public sealed class SealedItem
        {
            public int Value;
        }

        public class SealedOwner
        {
            public SealedItem Item;
        }

        public class OpenItem
        {
            public int Value;
        }

        public class OpenOwner
        {
            public OpenItem Item;
        }
    }
}
