using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using PinionCore.Network;
using PinionCore.Remote;
using PinionCore.Remote.Gateway.Servers;

namespace PinionCore.Remote.Gateway.Registrys
{
    public class Line
    {
        public readonly IStreamable Frontend;
        public readonly IStreamable Backend;

        public Line()
        {
            var stream = new Stream();
            Frontend = stream;            
            Backend = new PinionCore.Network.ReverseStream(stream);
        }
    }
}

