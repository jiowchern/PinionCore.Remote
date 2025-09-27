using PinionCore.Remote;
using System;
using System.Collections.Generic;

namespace PinionCore.Consoles.Chat1
{
    class ItemNotifier<T> : INotifier<T>
    {         
        readonly List<T> _Gpis;

        public IReadOnlyCollection<T> Items => _Gpis;
        

        public ItemNotifier()
        {
            _Gpis = new List<T>();
            _Supply += _Empty;
            _Unsupply += _Empty;
        }

        private void _Empty(T obj)
        {}

        public void Add(T item)
        {
            _Gpis.Add(item);
            _Supply(item);
        }
        public void Remove(T item)
        {
            if(_Gpis.Remove(item))
                _Unsupply(item);
        }
        event Action<T> _Supply;
        event Action<T> INotifier<T>.Supply
        {
            add
            {
                foreach (var gpi in _Gpis)
                {
                    value(gpi);
                }
                _Supply += value;
            }

            remove
            {
                _Supply -= value;
            }
        }

        event Action<T> _Unsupply;
        event Action<T> INotifier<T>.Unsupply
        {
            add
            {
                _Unsupply += value;
            }

            remove
            {
                _Unsupply -= value;
            }
        }

        internal void Clear()
        {
            foreach (var item in _Gpis)
            {
                _Unsupply(item);
            }
            _Gpis.Clear();
        }
    }
}

