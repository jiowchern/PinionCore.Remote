using System;
using System.Collections.Generic;
using PinionCore.Remote.Gateway.Protocols;
using PinionCore.Utility;

namespace PinionCore.Remote.Gateway.Registrys
{

    class UserLoginState : PinionCore.Utility.IStatus , ILoginable
    {
        private ICollection<ILoginable> _Logins;


        public event Action<uint, byte[]> DoneEvent;
        public UserLoginState(ICollection<ILoginable> logins)
        {
            _Logins = logins;
        }

        void IStatus.Enter()
        {
            _Logins.Add(this);
        }

        void IStatus.Leave()
        {
            _Logins.Remove(this);
        }

        PinionCore.Remote.Value ILoginable.Login(uint group, byte[] version)
        {
            DoneEvent(group, version);

            return new PinionCore.Remote.Value(false);
        }

        void IStatus.Update()
        {
            
        }
    }
   
}

