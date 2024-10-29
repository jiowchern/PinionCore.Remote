using System.Net.Sockets;

namespace PinionCore.Network
{
    public interface IPeer : IStreamable
    {
        event System.Action<SocketError> SocketErrorEvent;
    }
}
