using System;
using System.Collections.Generic;
using PinionCore.Network;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Registrys
{
    class LineAllocator : System.IDisposable
    {
        private readonly List<Line> _lines;
        private readonly Depot<IStreamable> _streams;

        public LineAllocator()
        {
            
            _lines = new List<Line>();
            _streams = new Depot<IStreamable>();
            StreamsNotifier = new Notifier<IStreamable>(_streams);
        }

        
        public Notifier<IStreamable> StreamsNotifier { get; }

        public IStreamable Alloc()
        {
            
            var line = new Line();
            _lines.Add(line);
            _streams.Items.Add(line.Backend);            
            return line.Frontend;
        }

        public void Free(IStreamable stream)
        {
            for (var i = 0; i < _lines.Count; i++)
            {
                if (_lines[i].Frontend != stream)
                {
                    continue;
                }

                _streams.Items.Remove(_lines[i].Backend);
                _lines.RemoveAt(i);
                return;
            }
        }

        public void Dispose()
        {
            _lines.Clear();
            _streams.Items.Clear();
        }
    }
}

