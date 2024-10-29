
using System;
using System.Linq;
using System.Reflection;
using PinionCore.Remote;

namespace PinionCore.Remote.Tests
{
}
namespace PinionCore.Utility.Client.JIT.Tests
{

    public class TestProtocol : PinionCore.Remote.IProtocol
    {
        byte[] IProtocol.VerificationCode => throw new System.NotImplementedException();

        Assembly IProtocol.Base => throw new System.NotImplementedException();

        Type[] IProtocol.SerializeTypes => throw new NotImplementedException();

        EventProvider IProtocol.GetEventProvider()
        {
            throw new System.NotImplementedException();
        }

        InterfaceProvider IProtocol.GetInterfaceProvider()
        {
            throw new System.NotImplementedException();
        }

        MemberMap IProtocol.GetMemberMap()
        {
            throw new System.NotImplementedException();
        }


    }

    public class AgentProivderTests
    {
        [NUnit.Framework.Test]
        public void CreateProtocol()
        {
            System.Type type = typeof(TestProtocol);

            IProtocol protocol = PinionCore.Remote.Protocol.ProtocolProvider.Create(type.Assembly).FirstOrDefault();
            NUnit.Framework.Assert.AreNotEqual(protocol, null);
        }

        [NUnit.Framework.Test]
        public void FindProtocols()
        {

            System.Type[] protocols = PinionCore.Remote.Protocol.ProtocolProvider.GetProtocols().ToArray();
            NUnit.Framework.Assert.AreEqual(typeof(TestProtocol), protocols[0]);
        }


    }
}
