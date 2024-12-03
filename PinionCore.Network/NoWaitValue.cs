using System;
using System.Runtime.CompilerServices;
using PinionCore.Remote;
namespace PinionCore.Network
{
    public class NoWaitValue<T> : IWaitableValue<T>, IAwaitable<T>
    {
        public readonly PinionCore.Remote.Value<T> Value;

        public NoWaitValue()
        {
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

            Value.OnValue +=(v)=> continuation?.Invoke();
        }
    }
}
