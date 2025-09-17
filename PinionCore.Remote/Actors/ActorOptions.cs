using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace PinionCore.Remote.Actors
{
    public sealed class ActorOptions
    {
        public static ActorOptions Default
        {
            get { return new ActorOptions(); }
        }

        public ActorOptions()
        {
            BoundedCapacity = DataflowBlockOptions.Unbounded;
            CancellationToken = CancellationToken.None;
            DisposeTimeout = Timeout.InfiniteTimeSpan;
        }

        public int BoundedCapacity { get; set; }

        public TaskScheduler TaskScheduler { get; set; }

        public CancellationToken CancellationToken { get; set; }

        public TimeSpan DisposeTimeout { get; set; }

        internal ActorOptions Clone()
        {
            return new ActorOptions
            {
                BoundedCapacity = BoundedCapacity,
                TaskScheduler = TaskScheduler,
                CancellationToken = CancellationToken,
                DisposeTimeout = DisposeTimeout
            };
        }
    }
}
