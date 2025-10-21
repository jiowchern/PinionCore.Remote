using System.Collections.Generic;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Hosts
{
    /// <summary>
    /// Defines the contract for selecting which <see cref="ILineAllocatable"/> should handle an incoming
    /// <see cref="IRoutableSession"/> for a particular group.
    /// </summary>
    public interface ISessionSelectionStrategy
    {
        /// <summary>
        /// Orders the available allocators for the given session and group according to the strategy.
        /// The coordinator will attempt to bind the session following the returned order.
        /// </summary>
        /// <param name="group">The group identifier for the requested allocator.</param>
        /// <param name="allocators">The current allocators registered for the group.</param>
        /// <returns>An ordered enumerable of allocators to try for binding.</returns>
        IEnumerable<Registrys.ILineAllocatable> OrderAllocators(uint group, IReadOnlyList<Registrys.ILineAllocatable> allocators);
    }
}

