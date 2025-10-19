using System;
using System.Collections.Generic;
using System.Reflection;
using PinionCore.Remote.Gateway.Protocols;
using PinionCore.Utility;

namespace PinionCore.Remote.Gateway.Registrys
{

    class User : IDisposable ,PinionCore.Utility.IUpdatable , IRegisterable
    {
        public readonly IBinder Binder;
        
        
        readonly StatusMachine _StatusMachine ;
        readonly ISoul _Soul;

        readonly NotifiableCollection<ILoginable> _Logins;
        readonly Notifier<ILoginable> _LoginNotifier;
        Notifier<ILoginable> IRegisterable.LoginNotifier => _LoginNotifier;

        readonly NotifiableCollection<IStreamProviable> _Streams;
        readonly Notifier<IStreamProviable> _StreamsNotifier ;
        Notifier<IStreamProviable> IRegisterable.StreamsNotifier => _StreamsNotifier;


        readonly ICollection<ILineAllocatable> _LineAllocators;

        public User(IBinder binder, ICollection<ILineAllocatable> lineAllocators)
        {
            _LineAllocators = lineAllocators;
            

            _Logins = new NotifiableCollection<ILoginable>();
            _LoginNotifier = new Notifier<ILoginable>(_Logins);

            _Streams = new NotifiableCollection<IStreamProviable>();
            _StreamsNotifier = new Notifier<IStreamProviable>(_Streams);

            _StatusMachine = new StatusMachine();
            
            Binder = binder;
            _Soul = binder.Bind<IRegisterable>(this);
        }

        public void Dispose()
        {
            Binder.Unbind(_Soul);
            _StatusMachine.Termination();
        }

        bool IUpdatable.Update()
        {
            _StatusMachine.Update();
            return true;
        }

        void IBootable.Launch()
        {
            _ToLogin();
        }

        private void _ToLogin()
        {
            var state = new UserLoginState(_Logins);
            state.DoneEvent += _ToAlloc;
            _StatusMachine.Push(state);
        }

        void _ToAlloc(uint group)
        {
            var state = new UserAllocState(group,_Streams, _LineAllocators);
            state.DoneEvent += _ToLogin;
            _StatusMachine.Push(state);
        }

        void IBootable.Shutdown()
        {
            _StatusMachine.Empty();
        }
    }
   
}

