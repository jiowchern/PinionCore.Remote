using System;
using System.Collections.Generic;
using PinionCore.Remote.Gateway.Protocols;
using PinionCore.Utility;

namespace PinionCore.Remote.Gateway.Hosts
{
    class UserVersionState : PinionCore.Utility.IStatus  , Protocols.IVerisable
    {
        readonly ICollection<IVerisable> _Versions;

        public event Action<byte[]> DoneEvent;
        public UserVersionState(ICollection<IVerisable> coll)
        {
            _Versions = coll;
        }
        void IStatus.Enter()
        {
            _Versions.Add(this); 
        }

        void IStatus.Leave()
        {
            _Versions.Remove(this);
        }

        void IVerisable.Set(byte[] version)
        {
            DoneEvent(version);
        }

        void IStatus.Update()
        {
            
        }
    }
}


