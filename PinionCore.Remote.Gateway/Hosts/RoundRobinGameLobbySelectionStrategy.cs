using System;
using System.Collections.Generic;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Hosts
{
    /// <summary>
    /// Provides a round-robin strategy for selecting lobbies within the same group.
    /// </summary>
    public sealed class RoundRobinGameLobbySelectionStrategy : IGameLobbySelectionStrategy
    {
        private readonly Dictionary<uint, int> _nextIndexByGroup = new Dictionary<uint, int>();

        public IEnumerable<Registrys.ILineAllocatable> OrderLobbies(uint group, IReadOnlyList<Registrys.ILineAllocatable> lobbies)
        {
            if (lobbies == null)
            {
                throw new ArgumentNullException(nameof(lobbies));
            }

            if (lobbies.Count == 0)
            {
                _nextIndexByGroup[group] = 0;
                return Array.Empty<Registrys.ILineAllocatable>();
            }

            var startIndex = GetStartIndex(group, lobbies.Count);
            var ordered = new Registrys.ILineAllocatable[lobbies.Count];
            for (var i = 0; i < lobbies.Count; i++)
            {
                ordered[i] = lobbies[(startIndex + i) % lobbies.Count];
            }

            _nextIndexByGroup[group] = (startIndex + 1) % lobbies.Count;
            return ordered;
        }

        private int GetStartIndex(uint group, int lobbyCount)
        {
            if (!_nextIndexByGroup.TryGetValue(group, out var index))
            {
                index = 0;
                _nextIndexByGroup[group] = index;
            }

            if (index >= lobbyCount)
            {
                index %= lobbyCount;
                _nextIndexByGroup[group] = index;
            }

            return index;
        }
    }
}

