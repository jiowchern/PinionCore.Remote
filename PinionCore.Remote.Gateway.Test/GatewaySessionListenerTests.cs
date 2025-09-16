using System.ComponentModel.DataAnnotations;
using NUnit.Framework;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway.Tests
{
    public class GatewaySessionListenerTests
    {
        [Test, Timeout(10000)]
        public void EnterEventTriggeredWhenClientJoins()
        {

            var stream = new PinionCore.Network.Stream();

            var reverseStream = new PinionCore.Network.ReverseStream(stream);
            var reverseSender = new PinionCore.Network.PackageSender(reverseStream, PinionCore.Memorys.PoolProvider.Shared);

            var reader = new PinionCore.Network.PackageReader(stream, PinionCore.Memorys.PoolProvider.Shared);
            var sender = new PinionCore.Network.PackageSender(stream, PinionCore.Memorys.PoolProvider.Shared);
            var virtualListener = new PinionCore.Remote.Gateway.Services.GatewaySessionListener(reader, sender);
            var listener = virtualListener as IListenable;

            bool enterFired = false;
            listener.StreamableEnterEvent += (s) =>
            {
                enterFired = true;
            };

            var joinPkg = new PinionCore.Remote.Gateway.Services.ClientToServerPackage
            {
                OpCode = PinionCore.Remote.Gateway.Services.OpCodeClientToServer.Join,
                Id = 1,
                Payload = new byte[0]
            };
            var serializer = new PinionCore.Remote.Gateway.Services.Serializer(PinionCore.Memorys.PoolProvider.Shared);

            var joinPkgBuf = serializer.Serialize(joinPkg);
            reverseSender.Push(joinPkgBuf);            

            while (!enterFired)
                virtualListener.HandlePackages();

            Assert.IsTrue(enterFired);
            
        }

        
    }
}

