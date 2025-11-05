using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using NUnit.Framework;
using PinionCore.Memorys;
using PinionCore.Network;

namespace PinionCore.Remote.Standalone.Test
{
    

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
   
}
