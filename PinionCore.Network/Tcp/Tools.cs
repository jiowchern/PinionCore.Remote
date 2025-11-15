using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace PinionCore.Network.Tcp
{
    public static class Tools
    {
        public static int GetAvailablePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        public static IEnumerable<int> GetAvailablePorts(int count)
        {
            var ports = new List<int>();
            var listeners = new List<TcpListener>();
            try
            {
                for (int i = 0; i < count; i++)
                {
                    var listener = new TcpListener(IPAddress.Loopback, 0);
                    listener.Start();
                    listeners.Add(listener);
                    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
                    ports.Add(port);
                }
            }
            finally
            {
                foreach (var listener in listeners)
                {
                    listener.Stop();
                }
            }
            return ports;
        }
    }
}
