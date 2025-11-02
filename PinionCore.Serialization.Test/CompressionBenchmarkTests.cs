using System;
using NUnit.Framework;

namespace PinionCore.Serialization.Tests
{
    public class CompressionBenchmarkTests
    {
        [Test]
        public void NegativeIntCompressionTest()
        {
            var finder = new DescribersFinder(typeof(int));
            var provider = new DescriberProvider(finder);
            var serializer = new Serializer(provider);

            // Test negative number compression with ZigZag
            var negativeOne = -1;
            var buffer = serializer.ObjectToBuffer(negativeOne);

            // With ZigZag: -1 encodes to 1, which takes 2 bytes (1 for type ID + 1 for data)
            // Without ZigZag: -1 would take 11 bytes (1 for type ID + 10 for 0xFFFFFFFF)
            Console.WriteLine($"Negative int (-1) size: {buffer.Count} bytes (with ZigZag)");
            Assert.LessOrEqual(buffer.Count, 3, "ZigZag should compress negative numbers efficiently");

            var result = (int)serializer.BufferToObject(buffer);
            Assert.AreEqual(-1, result);
        }

        [Test]
        public void StringCompressionTest()
        {
            var finder = new DescribersFinder(typeof(string), typeof(char), typeof(char[]));
            var provider = new DescriberProvider(finder);
            var serializer = new Serializer(provider);

            // Test ASCII string compression with UTF-8
            var testString = "HelloWorld";  // 10 ASCII characters
            var buffer = serializer.ObjectToBuffer(testString);

            // With UTF-8: 1 type ID + 1 length + 10 UTF-8 bytes = 12 bytes
            // Without UTF-8 (char array): 1 type ID + array overhead + (10 chars * 2-3 bytes each) ≈ 30+ bytes
            Console.WriteLine($"String '{testString}' size: {buffer.Count} bytes (with UTF-8)");
            Assert.LessOrEqual(buffer.Count, 15, "UTF-8 should compress ASCII strings efficiently");

            var result = serializer.BufferToObject(buffer) as string;
            Assert.AreEqual(testString, result);
        }

        [Test]
        public void StringWithUnicodeTest()
        {
            var finder = new DescribersFinder(typeof(string), typeof(char), typeof(char[]));
            var provider = new DescriberProvider(finder);
            var serializer = new Serializer(provider);

            // Test Unicode string
            var testString = "你好世界ABC";  // Chinese + ASCII
            var buffer = serializer.ObjectToBuffer(testString);

            Console.WriteLine($"Unicode string '{testString}' size: {buffer.Count} bytes (with UTF-8)");

            var result = serializer.BufferToObject(buffer) as string;
            Assert.AreEqual(testString, result);
        }

        [Test]
        public void ComprehensiveCompressionTest()
        {
            var finder = new DescribersFinder(typeof(TestStruct), typeof(int), typeof(string), typeof(char), typeof(char[]));
            var provider = new DescriberProvider(finder);
            var serializer = new Serializer(provider);

            var testData = new TestStruct
            {
                Id = -42,
                Name = "Player123",
                Score = -1000
            };

            var buffer = serializer.ObjectToBuffer(testData);
            Console.WriteLine($"TestStruct total size: {buffer.Count} bytes");
            Console.WriteLine($"  - Contains negative int: {testData.Id} (ZigZag optimized)");
            Console.WriteLine($"  - Contains string: '{testData.Name}' (UTF-8 optimized)");
            Console.WriteLine($"  - Contains negative int: {testData.Score} (ZigZag optimized)");

            var result = (TestStruct)serializer.BufferToObject(buffer);
            Assert.AreEqual(testData.Id, result.Id);
            Assert.AreEqual(testData.Name, result.Name);
            Assert.AreEqual(testData.Score, result.Score);
        }

        public struct TestStruct
        {
            public int Id;
            public string Name;
            public int Score;
        }
    }
}
