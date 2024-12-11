using System;
using System.Runtime.CompilerServices;
using PinionCore.Remote;
namespace PinionCore.Network
{
    public class NoWaitValue<T> : PinionCore.Remote.Value<T> , IWaitableValue<T>
    {
        public NoWaitValue() : base()
        {

        }

        public NoWaitValue(T value) : base(value)
        {

        }
    }
}
