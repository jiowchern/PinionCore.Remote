using PinionCore.Network;
using System.Net.Sockets;
using System.Threading.Tasks;
namespace PinionCore.Remote.Standalone
{
    public class ReverseStream : Network.ReverseStream
    {
        public ReverseStream(Network.Stream stream) : base(stream)
        {
        }
    }
}
