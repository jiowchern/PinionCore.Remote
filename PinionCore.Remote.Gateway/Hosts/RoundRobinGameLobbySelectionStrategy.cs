using System;
using System.Collections.Generic;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Hosts
{
    /// <summary>
    /// Default implementation that balances bindings by iterating lobbies in a round-robin order per group.
    /// </summary>
    public sealed class RoundRobinGameLobbySelectionStrategy : IGameLobbySelectionStrategy
    {
        private readonly Dictionary<uint, int> _nextIndices;

        public RoundRobinGameLobbySelectionStrategy()
        {
            _nextIndices = new Dictionary<uint, int>();
        }

        public IEnumerable<IGameLobby> Select(uint group, IReadOnlyList<IGameLobby> services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (services.Count == 0)
            {
                return Array.Empty<IGameLobby>();
            }

            if (!_nextIndices.TryGetValue(group, out var nextIndex))
            {
                nextIndex = 0;
            }

            var startIndex = nextIndex % services.Count;
            var ordered = new List<IGameLobby>(services.Count);
            for (var i = 0; i < services.Count; i++)
            {
                var index = (startIndex + i) % services.Count;
                ordered.Add(services[index]);
            }

            _nextIndices[group] = (startIndex + 1) % services.Count;

            return ordered;
        }
    }
}
