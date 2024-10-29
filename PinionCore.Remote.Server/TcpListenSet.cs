namespace PinionCore.Remote.Server
{
    public class TcpListenSet : ListenSet<PinionCore.Remote.Server.Tcp.Listener, Soul.IService>
    {
        public TcpListenSet(PinionCore.Remote.Server.Tcp.Listener listener, Soul.IService service) : base(listener, service) { }
    }
}
