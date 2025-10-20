using System;
using System.Collections.Generic;
using System.Text;
using PinionCore.Network;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway.Misc
{
    internal class NotifierListener :PinionCore.Remote.Soul.IListenable
    {
        readonly Notifier<Network.IStreamable> _notifier;
        public NotifierListener(Notifier<Network.IStreamable> notifier)
        {
            _notifier = notifier;
        }

        event Action<IStreamable> IListenable.StreamableEnterEvent
        {
            add
            {
                _notifier.Base.Supply += value;
            }

            remove
            {
                _notifier.Base.Supply -= value;
            }
        }

        event Action<IStreamable> IListenable.StreamableLeaveEvent
        {
            add
            {
                _notifier.Base.Unsupply += value;
            }

            remove
            {
                _notifier.Base.Unsupply -= value;
            }
        }
    }
}
