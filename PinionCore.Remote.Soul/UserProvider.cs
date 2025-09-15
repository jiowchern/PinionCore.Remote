using System;
using System.Collections.Generic;
using PinionCore.Memorys;

namespace PinionCore.Remote.Soul
{



    public class UserProvider : IDisposable
    {

        private readonly IListenable _Listenable;
        private readonly IPool _Pool;

        public event System.Action<Network.IStreamable, PinionCore.Network.PackageReader, PinionCore.Network.PackageSender> JoinEvent;
        public event System.Action<Network.IStreamable> LeaveEvent;

        public UserProvider(IListenable listenable, PinionCore.Memorys.IPool pool)
        {
            _Pool = pool;
            _Listenable = listenable;
            _Listenable.StreamableEnterEvent += _Join;
            _Listenable.StreamableLeaveEvent += _Leave;
        }

        void _Join(Network.IStreamable stream)
        {
            var reader = new PinionCore.Network.PackageReader(stream, _Pool);
            var sender = new PinionCore.Network.PackageSender(stream, _Pool);
            var join = JoinEvent;
            if (join != null)
            {
                join(stream, reader, sender);
            }
        }

        void _Leave(Network.IStreamable stream)
        {
            var leave = LeaveEvent;
            if (leave != null)
            {
                leave(stream);
            }
        }

        void IDisposable.Dispose()
        {
            _Listenable.StreamableEnterEvent -= _Join;
            _Listenable.StreamableLeaveEvent -= _Leave;
        }



    }
}

