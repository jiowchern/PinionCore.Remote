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

        public IEnumerable<IConnectionProvider> OrderLobbies(IReadOnlyList<IConnectionProvider> lobbies)
        {
            uint group = 0;
            if (lobbies == null)
            {
                throw new ArgumentNullException(nameof(lobbies));
            }

            if (lobbies.Count == 0)
            {
                _nextIndexByGroup.Remove(group);
                return Array.Empty<IConnectionProvider>();
            }

            if (!_nextIndexByGroup.TryGetValue(group, out var startIndex))
            {
                startIndex = 0;
            }

            if (startIndex >= lobbies.Count)
            {
                startIndex %= lobbies.Count;
            }

            var ordered = new IConnectionProvider[lobbies.Count];
            for (var i = 0; i < lobbies.Count; i++)
            {
                ordered[i] = lobbies[(startIndex + i) % lobbies.Count];
            }

            _nextIndexByGroup[group] = (startIndex + 1) % lobbies.Count;

            return ordered;
        }
    }
}
