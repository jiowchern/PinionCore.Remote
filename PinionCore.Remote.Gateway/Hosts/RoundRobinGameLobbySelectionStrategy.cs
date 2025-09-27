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
        private readonly List<int> _nextIndexByGroup = new List<int>();

        public IEnumerable<IConnectionProvider> OrderLobbies(IReadOnlyList<IConnectionProvider> lobbies)
        {
            uint group = 0;
            if (lobbies == null)
            {
                throw new ArgumentNullException(nameof(lobbies));
            }

            if (lobbies.Count == 0)
            {
                ResetGroupIndex(group);
                return Array.Empty<IConnectionProvider>();
            }

            var startIndex = GetStartIndex(group);

            if (startIndex >= lobbies.Count)
            {
                startIndex %= lobbies.Count;
            }

            var ordered = new IConnectionProvider[lobbies.Count];
            for (var i = 0; i < lobbies.Count; i++)
            {
                ordered[i] = lobbies[(startIndex + i) % lobbies.Count];
            }

            SetNextIndex(group, (startIndex + 1) % lobbies.Count);

            return ordered;
        }

        private int GetStartIndex(uint group)
        {
            EnsureGroupIndex(group);
            return _nextIndexByGroup[(int)group];
        }

        private void SetNextIndex(uint group, int nextIndex)
        {
            EnsureGroupIndex(group);
            _nextIndexByGroup[(int)group] = nextIndex;
        }

        private void ResetGroupIndex(uint group)
        {
            if (group > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(group));
            }

            if (_nextIndexByGroup.Count > group)
            {
                _nextIndexByGroup[(int)group] = 0;
            }
        }

        private void EnsureGroupIndex(uint group)
        {
            if (group > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(group));
            }

            while (_nextIndexByGroup.Count <= group)
            {
                _nextIndexByGroup.Add(0);
            }
        }
    }
}
