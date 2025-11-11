using System;
using NUnit.Framework;
using PinionCore.Network;
using PinionCore.Remote.Ghost;

namespace PinionCore.Remote.Standalone.Test
{
    public class SoulTest
    {
        [NUnit.Framework.Test,Timeout(10000)]
        public void ServiceTest()
        {
            

            IEntry entry = NSubstitute.Substitute.For<IEntry>();
            Soul.IListenable listenable = NSubstitute.Substitute.For<Soul.IListenable>();
            IGpiA gpia = new SoulGpiA();
            entry.RegisterClientBinder(NSubstitute.Arg.Do<IBinder>(binder => binder.Bind<IGpiA>(gpia)));

            ISerializable serializer = new PinionCore.Remote.DynamicSerializer();
            IProtocol protocol = ProtocolHelper.CreateProtocol();
            var internalSer = new PinionCore.Remote.InternalSerializer();
            Memorys.Pool pool = PinionCore.Memorys.PoolProvider.Shared;
            
            Soul.IService service = new PinionCore.Remote.Soul.ServiceUpdateLoop(new Soul.SessionEngine(entry, protocol, serializer, internalSer, pool));

            var ghostAgent = new PinionCore.Remote.Ghost.User(protocol, serializer, internalSer, pool);
            var ghostAgentDisconnect = ghostAgent.Connect(service);
            IAgent agent = ghostAgent;
            IGpiA ghostGpia = null;

            //NSubstitute.Core.Events.DelegateEventWrapper<Action<IStreamable>> wrapper = NSubstitute.Raise.Event<System.Action<IStreamable>>(serverStream);
            //listenable.StreamableEnterEvent += wrapper;


            agent.QueryNotifier<IGpiA>().Supply += gpi => ghostGpia = gpi;

            while (ghostGpia == null)
            {
                agent.HandleMessage();
                agent.HandlePackets();
            }
            ghostAgent.Disable();
            agent.Disable();
            //listenable.StreamableLeaveEvent -= wrapper;

            IDisposable disposable = service;
            disposable.Dispose();
        }
    }
}
