using System.Collections.Generic;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Hosts
{
    /// <summary>
    /// Defines the contract for selecting which <see cref="IConnectionLobby"/> should handle an incoming
    /// <see cref="IRoutableSession"/> for a particular group.
    /// </summary>
    public interface IGameLobbySelectionStrategy
    {
        /// <summary>
        /// Orders the available lobbies for the given session and group according to the strategy.
        /// The coordinator will attempt to bind the session following the returned order.
        /// </summary>
        /// <param name="group">The group identifier for the requested lobby.</param>
        /// <param name="lobbies">The current lobbies registered for the group.</param>
        /// <returns>An ordered enumerable of lobbies to try for binding.</returns>
        IEnumerable<IConnectionLobby> OrderLobbies(IReadOnlyList<IConnectionLobby> lobbies);
    }
}
