using System;
using NUnit.Framework;
using PinionCore.Network;
using PinionCore.Remote.Ghost;
using PinionCore.Remote.Server;
using PinionCore.Remote.Client;

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
            entry.OnSessionOpened(NSubstitute.Arg.Do<ISessionBinder>(binder => binder.Bind<IGpiA>(gpia)));

            ISerializable serializer = new PinionCore.Remote.DynamicSerializer();
            IProtocol protocol = ProtocolHelper.CreateProtocol();
            var internalSer = new PinionCore.Remote.InternalSerializer();
            Memorys.Pool pool = PinionCore.Memorys.PoolProvider.Shared;
            
            Soul.IService service = new PinionCore.Remote.Soul.ServiceUpdateLoop(new Soul.SessionEngine(entry, protocol, serializer, internalSer, pool));
            var standaloneEndpoint = new PinionCore.Remote.Standalone.ListeningEndpoint();
            var (listenHandle, listenErrors) = service.ListenAsync(standaloneEndpoint).GetAwaiter().GetResult();
            Assert.IsEmpty(listenErrors, "Standalone listener failed.");

            var ghostAgent = new PinionCore.Remote.Ghost.User(protocol, serializer, internalSer, pool);
            var connectable = (PinionCore.Remote.Client.IConnectingEndpoint)standaloneEndpoint;
            var stream = connectable.ConnectAsync().GetAwaiter().GetResult();
            ghostAgent.Enable(stream);
            IAgent agent = ghostAgent;
            IGpiA ghostGpia = null;

            //NSubstitute.Core.Events.DelegateEventWrapper<Action<IStreamable>> wrapper = NSubstitute.Raise.Event<System.Action<IStreamable>>(serverStream);
            //listenable.StreamableEnterEvent += wrapper;


            agent.QueryNotifier<IGpiA>().Supply += gpi => ghostGpia = gpi;

            while (ghostGpia == null)
            {
                agent.HandleMessages();
                agent.HandlePackets();
            }
            ghostAgent.Disable();
            agent.Disable();
            listenHandle.Dispose();
            ((IDisposable)standaloneEndpoint).Dispose();
            //listenable.StreamableLeaveEvent -= wrapper;

            IDisposable disposable = service;
            disposable.Dispose();
        }
    }
}
