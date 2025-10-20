using System;
using System.Collections.Generic;
using PinionCore.Network;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Registrys
{
    class LineAllocator : System.IDisposable, ILineAllocatable
    {
        private readonly List<Line> _lines;
        private readonly NotifiableCollection<IStreamable> _streams;

        public LineAllocator(uint id)
        {
            Group = id;
            _lines = new List<Line>();
            _streams = new NotifiableCollection<IStreamable>();
            StreamsNotifier = new Notifier<IStreamable>(_streams);
        }

        public uint Group { get; }
        public Notifier<IStreamable> StreamsNotifier { get; }

        public IStreamable Alloc()
        {
            Console.WriteLine($"Allocating line for group {Group}");
            var line = new Line();
            _lines.Add(line);
            _streams.Items.Add(line.Backend);
            Console.WriteLine($"Backend supplied for group {Group}");
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

