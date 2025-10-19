using System;
using System.Collections.Generic;

namespace PinionCore.Remote.Gateway.Hosts
{
    class CompositeNotifier<T> : INotifier<T> , INotifierOwneable
    {
        readonly List<Action<T>> _SupplyActions;
        readonly List<Action<T>> _UnsupplyActions;
        readonly List<INotifierQueryable> _Notifiers;
        public CompositeNotifier() {
            _Notifiers = new List<INotifierQueryable>();
            _SupplyActions = new List<Action<T>>();
            _UnsupplyActions = new List<Action<T>>();
        }

        public void AddNotifier(INotifierQueryable notifierQueryable)
        {
            _Notifiers.Add(notifierQueryable);
            var notifier = notifierQueryable.QueryNotifier<T>();
            foreach(var supply in _SupplyActions)
            {
                notifier.Supply += supply;
            }
            foreach(var unsupply in _UnsupplyActions)
            {
                notifier.Unsupply += unsupply;
            }
        }

        public void RemoveNotifier(INotifierQueryable notifierQueryable)
        {
            _Notifiers.Remove(notifierQueryable);
            var notifier = notifierQueryable.QueryNotifier<T>();
            foreach (var supply in _SupplyActions)
            {
                notifier.Supply -= supply;
            }
            foreach (var unsupply in _UnsupplyActions)
            {
                notifier.Unsupply -= unsupply;
            }
        }

        event Action<T> INotifier<T>.Supply
        {
            add
            {
                _SupplyActions.Add(value);
                foreach(var notifier in _Notifiers)
                {
                    var n = notifier.QueryNotifier<T>();
                    n.Supply += value;
                }
            }

            remove
            {
                foreach (var notifier in _Notifiers)
                {
                    var n = notifier.QueryNotifier<T>();
                    n.Supply -= value;
                }
                _SupplyActions.Remove(value);
                
            }
        }

        event Action<T> INotifier<T>.Unsupply
        {
            add
            {
                _UnsupplyActions.Add(value);
                foreach (var notifier in _Notifiers)
                {
                    var n = notifier.QueryNotifier<T>();
                    n.Unsupply += value;
                }
            }

            remove
            {
                foreach (var notifier in _Notifiers)
                {
                    var n = notifier.QueryNotifier<T>();
                    n.Unsupply -= value;
                }
                _UnsupplyActions.Remove(value);
            }

        }
    }
}



