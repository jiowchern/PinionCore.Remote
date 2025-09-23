using System.Collections.Generic;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Hosts
{
    /// <summary>
    /// Defines a strategy for ordering <see cref="IGameLobby"/> instances when binding sessions.
    /// </summary>
    public interface IGameLobbySelectionStrategy
    {
        /// <summary>
        /// Produces an ordered set of candidate lobbies for the specified group.
        /// </summary>
        /// <param name="group">The group identifier for the requested binding.</param>
        /// <param name="services">The currently registered lobbies for the group.</param>
        /// <returns>An ordered sequence of lobbies to probe for binding.</returns>
        IEnumerable<IGameLobby> Select(uint group, IReadOnlyList<IGameLobby> services);
    }
}
