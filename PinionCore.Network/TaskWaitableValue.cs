


using System;
using System.Runtime.CompilerServices;

using System.Threading.Tasks;

namespace PinionCore.Network
{
    using PinionCore.Remote;
    public class TaskWaitableValue<T> : IAwaitableSource<T>, IAwaitable<T>
    {
        readonly PinionCore.Remote.Value<T> _Value;
        readonly System.Threading.Tasks.Task<T> _Task;

        bool IAwaitable<T>.IsCompleted => _Task.GetAwaiter().IsCompleted;

        IAwaitable<T> IAwaitableSource<T>.GetAwaiter()
        {
            return this;
        }
        public TaskWaitableValue(System.Threading.Tasks.Task<T> task)
        {
            _Task = task;
            _Value = new Remote.Value<T>();
            _Set(task);
        }

        private async void _Set(Task<T> task)
        {
            T val = await task;
            _Value.SetValue(val);
        }

        T IAwaitable<T>.GetResult()
        {
            return _Task.GetAwaiter().GetResult();
        }

        void INotifyCompletion.OnCompleted(Action continuation)
        {
            _Task.GetAwaiter().OnCompleted(continuation);
        }

    }
}
