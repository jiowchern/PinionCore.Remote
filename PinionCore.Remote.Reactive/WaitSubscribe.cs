using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;


namespace PinionCore.Remote.Reactive
{
    public class WaitSubscribe<T>
    {
        public static WaitSubscribe<T> Create(IObservable<T> obs)
        {
            var ws = new WaitSubscribe<T>();
            obs.Subscribe(ws.Get);
            return ws;
        }

        public readonly System.Collections.Concurrent.ConcurrentQueue<T> Values;
        public WaitSubscribe()
        {
            Values = new System.Collections.Concurrent.ConcurrentQueue<T>();
        }

        public void Get(T gpi)
        {
            Values.Enqueue(gpi);
        }

        public IEnumerable<T> WaitUntil(System.Func<System.Collections.Generic.IList<T>, bool> func, TimeSpan timeout)
        {
            System.Threading.SpinWait.SpinUntil(() =>
            {
                return func(Values.ToList());
            }, timeout);
            return Values;
        }
    }
}
