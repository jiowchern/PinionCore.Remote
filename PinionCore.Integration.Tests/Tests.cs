using System.Linq;
using NUnit.Framework;
using PinionCore.Remote;

namespace PinionCore.Integration.Tests
{
    public class Tests
    {


        [Test]
        public void InterfaceProviderTyepsTest()
        {
            var types = new System.Collections.Generic.Dictionary<System.Type, System.Type>();
            types.Add(typeof(IType), typeof(CType));
            var ip = new InterfaceProvider(types);

            System.Type type = ip.Types.First();

            Assert.AreEqual(type, typeof(IType));


        }


        [Test]
        public void AgentEventRectifierSupplyTest()
        {
            var types = new System.Collections.Generic.Dictionary<System.Type, System.Type>();
            types.Add(typeof(IType), typeof(CType));
            var ip = new InterfaceProvider(types);

            var agent = new TestAgent();
            var cType = new CType(1);

            object outSupplyInstance = null;

            using (var rectifier = new Remote.Client.AgentEventRectifier(ip.Types, agent))
            {

                rectifier.SupplyEvent += (type, instance) =>
                {
                    outSupplyInstance = instance;
                };

                agent.Add(typeof(IType), cType);

            }
            Assert.AreEqual(outSupplyInstance, cType);

        }

        [Test]
        public void AgentEventRectifierUnsupplyTest()
        {
            var types = new System.Collections.Generic.Dictionary<System.Type, System.Type>();
            types.Add(typeof(IType), typeof(CType));
            var ip = new InterfaceProvider(types);


            var agent = new TestAgent();
            var cType = new CType(1);

            object outInstance = null;
            System.Type outType = null;
            using (var rectifier = new Remote.Client.AgentEventRectifier(ip.Types, agent))
            {
                rectifier.SupplyEvent += (type, instance) => { };
                rectifier.UnsupplyEvent += (type, instance) =>
                {
                    outInstance = instance;
                    outType = type;
                };

                agent.Add(typeof(IType), cType);

                agent.Remove(typeof(IType), cType);

            }
            Assert.AreEqual(outInstance, cType);
            Assert.AreEqual(typeof(IType), outType);

        }
        [Test]
        public void MethodStringInvokerTest1()
        {
            var test = new CType(1);
            System.Reflection.MethodInfo method = typeof(IType).GetMethod(nameof(IType.TestMethod1));
            var invoker = new Remote.Client.MethodStringInvoker(test, method, new Remote.Client.TypeConverterSet());
            invoker.Invoke("1", "2", "3");

            Assert.AreEqual(true, test.TestMethod1Invoked);


        }

        [Test]
        public void AgentCommandTest1()
        {
            var test = new CType(1);
            System.Reflection.MethodInfo method = typeof(IType).GetMethod(nameof(IType.TestMethod1));
            var invoker = new Remote.Client.MethodStringInvoker(test, method, new Remote.Client.TypeConverterSet());
            var agentCommand = new Remote.Client.AgentCommand(new Remote.Client.AgentCommandVersionProvider(), typeof(IType), invoker);
            Assert.AreEqual("IType-0.TestMethod1 [a1,a2,a3]", agentCommand.Name);

        }
        [Test]
        public void AgentCommandRegisterTest1()
        {

            var command = new Utility.Command();
            var regCount = 0;
            command.RegisterEvent += (cmd, ret, args) =>
            {
                regCount++;
            };
            var unregCount = 0;
            command.UnregisterEvent += (cmd) =>
            {
                unregCount++;
            };
            var agentCommandRegister = new Remote.Client.AgentCommandRegister(command, new Remote.Client.TypeConverterSet());

            var test = new CType(1);

            agentCommandRegister.Register(typeof(IType), test);
            agentCommandRegister.Unregister(test);

            Assert.AreEqual(2, regCount);
            Assert.AreEqual(2, unregCount);
        }

    }
}
