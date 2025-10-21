using System;
using System.Collections.Generic;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Hosts
{
    /// <summary>
    /// Provides a round-robin strategy for selecting allocators within the same group.
    /// </summary>
    public sealed class RoundRobinSelector : ISessionSelectionStrategy
    {
        private readonly Dictionary<uint, int> _nextIndexByGroup = new Dictionary<uint, int>();

        public IEnumerable<Registrys.ILineAllocatable> OrderAllocators(uint group, IReadOnlyList<Registrys.ILineAllocatable> allocators)
        {
            if (allocators == null)
            {
                throw new ArgumentNullException(nameof(allocators));
            }

            if (allocators.Count == 0)
            {
                _nextIndexByGroup[group] = 0;
                return Array.Empty<Registrys.ILineAllocatable>();
            }

            var startIndex = GetStartIndex(group, allocators.Count);
            var ordered = new Registrys.ILineAllocatable[allocators.Count];
            for (var i = 0; i < allocators.Count; i++)
            {
                ordered[i] = allocators[(startIndex + i) % allocators.Count];
            }

            _nextIndexByGroup[group] = (startIndex + 1) % allocators.Count;
            return ordered;
        }

        private int GetStartIndex(uint group, int allocatorCount)
        {
            if (!_nextIndexByGroup.TryGetValue(group, out var index))
            {
                index = 0;
                _nextIndexByGroup[group] = index;
            }

            if (index >= allocatorCount)
            {
                index %= allocatorCount;
                _nextIndexByGroup[group] = index;
            }

            return index;
        }
    }
}

