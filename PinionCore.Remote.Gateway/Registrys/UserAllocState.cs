using System.Collections.Generic;
using PinionCore.Network;
using PinionCore.Remote.Gateway.Protocols;
using PinionCore.Utility;

namespace PinionCore.Remote.Gateway.Registrys
{
    class UserAllocState : PinionCore.Utility.IStatus , IStreamProviable
    {
        private readonly ICollection<ILineAllocatable> _Allocators;
        readonly LineAllocator _Allocator;
        
        
        readonly ICollection<IStreamProviable> _StreamProviables;
        
        public UserAllocState(uint id,ICollection<IStreamProviable> streamsProvider,ICollection<ILineAllocatable> lineAllocators)
        {
            _Allocators = lineAllocators;
            _Allocator = new LineAllocator(id);
            _StreamProviables = streamsProvider;
            


        }


        Notifier<IStreamable> IStreamProviable.Streams => _Allocator.StreamsNotifier;

        void IStatus.Enter()
        {
            _StreamProviables.Add(this);
            _Allocators.Add(_Allocator);
            
        }

        public event System.Action DoneEvent;
        void IStreamProviable.Exit()
        {
            DoneEvent();
        }

        void IStatus.Leave()
        {
            _Allocators.Remove(_Allocator);
            _StreamProviables.Remove(this);
            _Allocator.Dispose();
        }
        void IStatus.Update()
        {
            
        }
    }
   
}

