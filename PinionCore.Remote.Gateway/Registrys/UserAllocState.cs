using System.Collections.Generic;
using PinionCore.Network;
using PinionCore.Remote.Gateway.Protocols;
using PinionCore.Utility;

namespace PinionCore.Remote.Gateway.Registrys
{
    class UserAllocState : PinionCore.Utility.IStatus , IStreamProviable, ILineAllocatable
    {
        private readonly ICollection<ILineAllocatable> _Allocators;
        readonly LineAllocator _Allocator;
        
        
        readonly ICollection<IStreamProviable> _StreamProviables;

        readonly uint _Group;
        readonly byte[] _Version;
        public UserAllocState(byte[] version, uint group,ICollection<IStreamProviable> streamsProvider,ICollection<ILineAllocatable> lineAllocators)
        {
            _Allocators = lineAllocators;
            _Allocator = new LineAllocator();
            _StreamProviables = streamsProvider;

            _Group = group;
            _Version = version;
        }


        Notifier<IStreamable> IStreamProviable.Streams => _Allocator.StreamsNotifier;

        byte[] ILineAllocatable.Version => _Version;

        uint ILineAllocatable.Group => _Group;

        void IStatus.Enter()
        {
            _StreamProviables.Add(this);
            _Allocators.Add(this);
            
        }

        public event System.Action DoneEvent;
        void IStreamProviable.Exit()
        {
            DoneEvent();
        }

        void IStatus.Leave()
        {
            _Allocators.Remove(this);
            _StreamProviables.Remove(this);
            _Allocator.Dispose();
        }
        void IStatus.Update()
        {
            
        }

        IStreamable ILineAllocatable.Alloc()
        {
            return _Allocator.Alloc();
        }

        void ILineAllocatable.Free(IStreamable stream)
        {
            _Allocator.Free(stream);
        }
    }
   
}

