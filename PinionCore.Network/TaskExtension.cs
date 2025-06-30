


namespace PinionCore.Network
{
    using PinionCore.Remote;
    public static class TaskExtension
    {
        public static IAwaitableSource<T> ToWaitableValue<T>(this System.Threading.Tasks.Task<T> task)
        {
            return new TaskWaitableValue<T>(task);
        }

        public static IAwaitableSource<T> ToWaitableValue<T>(this T val)
        {
            return new NoWaitValue<T>(val);
        }
    }
}
