using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using PinionCore.Network;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Registrys
{
    class LineAllocator : System.IDisposable , ILineAllocatable
    {
        
        readonly List<Line> _Lines;
        public LineAllocator(uint id)
        {
            Group = id;
            _Lines = new List<Line>();
            
            _Streams = new NotifiableCollection<IStreamable>();
            StreamsNotifier = new Notifier<IStreamable>(_Streams);
        }

        readonly NotifiableCollection<IStreamable> _Streams;
        public readonly Notifier<IStreamable> StreamsNotifier;
        

        public uint Group { get; }
        

        public IStreamable Alloc()
        {
            var line = new Line();            
            _Streams.Items.Add(line.Backend);
            return line.Frontend;
        }
        public void Free(IStreamable stream)
        {
            for(int i = 0; i < _Lines.Count; i++)
            {
                if(_Lines[i].Frontend == stream)
                {
                    _Streams.Items.Remove(_Lines[i].Backend);
                    _Lines.RemoveAt(i);
                    return;
                }
            }
        }

        public void Dispose()
        {
            // free all
            _Lines.Clear();
            _Streams.Items.Clear();
        }
    }
}

