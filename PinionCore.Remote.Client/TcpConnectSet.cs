namespace PinionCore.Remote.Client
{
    public class TcpConnectSet : ConnectSet<PinionCore.Network.Tcp.Connector>
    {
        public TcpConnectSet(PinionCore.Network.Tcp.Connector connecter, Remote.Ghost.IAgent agent) : base(connecter, agent) { }
    }
}
