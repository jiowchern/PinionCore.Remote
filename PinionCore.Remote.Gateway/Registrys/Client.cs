using System;
using System.Reactive.Linq;
using PinionCore.Network;
using PinionCore.Remote.Gateway.Protocols;
using PinionCore.Remote.Ghost;
using PinionCore.Remote.Reactive;

namespace PinionCore.Remote.Gateway.Registrys
{
    class Client : IDisposable
    {
        readonly uint Group;
        readonly INotifierQueryable _Queryer;

        readonly NotifiableCollection<IStreamable> _Streams;
        public readonly Notifier<IStreamable> Notifier;
        public readonly IAgent Agent;
        private readonly IDisposable _Dispose;

        public Client(uint group)
        {
            _Streams = new NotifiableCollection<IStreamable>();
            Notifier = new Notifier<IStreamable>(_Streams);

            Group = group;            
            Agent = Protocols.Provider.CreateAgent();
            _Queryer = Agent;

            var obs = from r in _Queryer.QueryNotifier<IRegisterable>().SupplyEvent()
                      from l in r.LoginNotifier.SupplyEvent()
                      from ret in l.Login(Group).RemoteValue()
                      from s in r.StreamsNotifier.SupplyEvent()
                      select s;

            _Dispose = obs.Subscribe(_Set);

        }

        private void _Set(IStreamProviable proviable)
        {
            Console.WriteLine("Client received stream provider");

            if(_Streams.Items.Count > 0)
                throw new Exception("Already registered stream provider.");
            
            proviable.Streams.Base.Supply += _Streams.Items.Add;
            proviable.Streams.Base.Supply += s => Console.WriteLine("Client stream supplied");
            proviable.Streams.Base.Unsupply += (s) => { _Streams.Items.Remove(s); };
        }
        

        public void Dispose()
        {
            _Streams.Items.Clear();
            _Dispose.Dispose();
        }
    }
}




