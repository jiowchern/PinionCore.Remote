using NSubstitute;
using PinionCore.Remote.ProviderHelper;
using System;
using System.Linq;

namespace PinionCore.Remote.Tools.Protocol.Sources.TestCommon.Tests
{
    
    public class GhostProviderTests
    {
        static bool _HasSub = false;
        [NUnit.Framework.Test]
        public void AuroReleaseTest()
        {

            var protocol = PinionCore.Remote.Tools.Protocol.Sources.TestCommon.ProtocolProvider.CreateCase1();
            var es = new PinionCore.Remote.Serializer(protocol.SerializeTypes);
            IInternalSerializable iniers = new PinionCore.Remote.InternalSerializer();
            var exchanger = new OpCodeExchanger();
            var ghostOwner = new GhostsOwner(protocol);
            var provider = new PinionCore.Remote.GhostProviderQueryer(protocol, es, iniers, ghostOwner);
            ClientExchangeable providerExchange = provider;
            System.Collections.Generic.List<PinionCore.Remote.ClientToServerOpCode> opcodes = new System.Collections.Generic.List<PinionCore.Remote.ClientToServerOpCode>();
            providerExchange.ResponseEvent += (code , buf) => {
                opcodes.Add(code);
            };
            provider.Start();

            IMethodable methodable = null;
            provider.QueryProvider<IMethodable>().Supply += (gpi) =>
            {
                methodable = gpi;
            };


            {
                var package = new PinionCore.Remote.Packages.PackageLoadSoul();
                package.TypeId = protocol.GetMemberMap().GetInterface(typeof(IMethodable));
                package.EntityId = 1;
                package.ReturnType = false;

                providerExchange.Request(ServerToClientOpCode.LoadSoul, iniers.Serialize(package));

            }

            {
                var package = new PinionCore.Remote.Packages.PackageLoadSoulCompile();
                package.TypeId = protocol.GetMemberMap().GetInterface(typeof(IMethodable));
                package.EntityId = 1;
                package.ReturnId = 0;
                providerExchange.Request(ServerToClientOpCode.LoadSoulCompile, iniers.Serialize(package));
            }
            

            _HasSub = false;
            _SetHasSub(methodable);

            {
                var package = new PinionCore.Remote.Packages.PackageLoadSoul();
                package.TypeId = protocol.GetMemberMap().GetInterface(typeof(IMethodable));
                package.EntityId = 2;
                package.ReturnType = true;
                providerExchange.Request(ServerToClientOpCode.LoadSoul, iniers.Serialize(package));

            }

            {
                var package = new PinionCore.Remote.Packages.PackageLoadSoulCompile();
                package.TypeId = protocol.GetMemberMap().GetInterface(typeof(IMethodable));
                package.EntityId = 2;
                package.ReturnId = 1;
                providerExchange.Request(ServerToClientOpCode.LoadSoulCompile, iniers.Serialize(package));
            }

            {
                var package = new PinionCore.Remote.Packages.PackageUnloadSoul();
                package.EntityId = 1;

                providerExchange.Request(ServerToClientOpCode.UnloadSoul, iniers.Serialize(package));
            }


            GC.Collect(2, GCCollectionMode.Forced, true);
            
            {
                providerExchange.Request(ServerToClientOpCode.Ping, iniers.Serialize(new byte[0]));
            }
            
            

            provider.Stop();
            NUnit.Framework.Assert.True(_HasSub);
            NUnit.Framework.Assert.AreNotEqual(null, opcodes.Any(o => o == ClientToServerOpCode.Release));
        }

        

        private static void _SetHasSub(IMethodable methodable)
        {
            methodable.GetValueSelf().OnValue += (gpi) => _HasSub = true;
            
        }

    }
}