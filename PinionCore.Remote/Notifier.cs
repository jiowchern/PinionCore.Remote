using System;
using System.Linq;

namespace PinionCore.Remote
{
    public class Notifier<T> : IObjectAccessible, ITypeObjectNotifiable, System.IDisposable where T : class
    {

        public readonly INotifier<T> Base;
        readonly System.Collections.Generic.ICollection<T> _Collection;
        
        readonly PinionCore.Remote.Depot<TypeObject> _TypeObjects;

        public Notifier(INotifier<T> notifier, System.Collections.Generic.ICollection<T> collection)
        {
            _TypeObjects = new Depot<TypeObject>();
            _Collection = collection;        
            Base = notifier;
            Base.Supply += _OnSupply;
            Base.Unsupply += _OnUnsupply;
        }
        public Notifier(Depot<T> collection) : this(collection, collection)
        {

        }

        // 供伺服器端把既有資料來源（如以父介面曝光的 Depot）包裝成 Notifier。
        // collection 只在 Ghost 端經由 IObjectAccessible 使用，此情境不會被呼叫。
        public Notifier(INotifier<T> notifier) : this(notifier, new System.Collections.Generic.List<T>())
        {
        }

        public Notifier() : this(new Depot<T>())
        {
        }

        private void _OnUnsupply(T obj)
        {


            lock (_TypeObjects)
            {
                System.Collections.Generic.IEnumerable<TypeObject> items = from item in _TypeObjects.Items where item.Instance == obj && item.Type == typeof(T) select item;
                TypeObject i = items.First();
                _TypeObjects.Items.Remove(i);
            }

        }

        private void _OnSupply(T obj)
        {

            var to = new TypeObject(typeof(T), obj);
            lock (_TypeObjects)
                _TypeObjects.Items.Add(to);
        }


        event Action<TypeObject> ITypeObjectNotifiable.SupplyEvent
        {
            add
            {

                _TypeObjects.Notifier.Supply += value;
            }

            remove
            {
                _TypeObjects.Notifier.Supply -= value;
            }
        }

        public void Dispose()
        {
            Base.Supply -= _OnSupply;
            Base.Unsupply -= _OnUnsupply;
        }


        event Action<TypeObject> ITypeObjectNotifiable.UnsupplyEvent
        {
            add
            {
                _TypeObjects.Notifier.Unsupply += value;
            }

            remove
            {
                _TypeObjects.Notifier.Unsupply -= value;
            }
        }

        void IObjectAccessible.Add(object instance)
        {
            _Collection.Add((T)instance);
        }

        void IObjectAccessible.Remove(object instance)
        {
            _Collection.Remove((T)instance);
        }


        void IDisposable.Dispose()
        {
            Dispose();
        }
    }
}
