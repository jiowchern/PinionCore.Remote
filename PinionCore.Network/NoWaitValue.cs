using System;
using System.Runtime.CompilerServices;
using PinionCore.Remote;
namespace PinionCore.Network
{
    public class NoWaitValue<T> : IWaitableValue<T>, IAwaitable<T>
    {
        public readonly PinionCore.Remote.Value<T> Value;
        public readonly System.Collections.Concurrent.ConcurrentQueue<T> _Invokeds;
        int _InvokeCount;
        public NoWaitValue()
        {
            _Invokeds = new System.Collections.Concurrent.ConcurrentQueue<T>();
            _InvokeCount = 0;
            Value = new Remote.Value<T>();
        }
        public NoWaitValue(T val)
        {
            Value = new Remote.Value<T>(val);
        }

        bool IAwaitable<T>.IsCompleted => Value.HasValue();

        event Action<T> IWaitableValue<T>.ValueEvent
        {
            add
            {
                Value.OnValue += value;
            }

            remove
            {
                Value.OnValue -= value;
            }
        }

        IAwaitable<T> IWaitableValue<T>.GetAwaiter()
        {
            return this;
        }

        T IAwaitable<T>.GetResult()
        {
            return Value.GetValue();
        }

        void INotifyCompletion.OnCompleted(Action continuation)
        {

            void Handler(T v)
            {
                _Invokeds.Enqueue(v);

                var count = System.Threading.Interlocked.Increment(ref _InvokeCount);
                if (count > 1)
                {
                    PinionCore.Utility.Log.Instance.WriteInfo($" INotifyCompletion.OnCompleted:{count} ");

                    throw new InvalidOperationException("INotifyCompletion.OnCompleted");
                }

                Value.OnValue -= Handler;

                continuation?.Invoke();

            }
            Value.OnValue += Handler;
        }
    }
}
