using System.Net.Sockets;

namespace PinionCore.Remote.Client.Tcp
{
    public interface IOnlineable
    {
        event System.Action<SocketError> ErrorEvent;
        void Disconnect();
    }
}
