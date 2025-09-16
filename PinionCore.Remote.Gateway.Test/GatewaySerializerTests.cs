using NUnit.Framework;

namespace PinionCore.Remote.Gateway.Tests
{
    public class GatewaySerializerTests
    {
        [Test]
        public void SerializerTest()
        {

            var serializer = new PinionCore.Remote.Gateway.Serializer(PinionCore.Memorys.PoolProvider.Shared, new System.Type[]
            {
                typeof(Package),
                typeof(uint),
                typeof(byte[]),
                typeof(byte)
            });


            var package = new PinionCore.Remote.Gateway.Package()
            {
                ServiceId = 42,
                Payload = new byte[] { 1, 2, 3, 4, 5 }
            };

            var serialized = serializer.Serialize(package) ;
            var deserialized = (Package)serializer.Deserialize(serialized)  ;

            Assert.AreEqual(package.ServiceId, deserialized.ServiceId);
            Assert.AreEqual(package.Payload, deserialized.Payload);

        }
    }
}

