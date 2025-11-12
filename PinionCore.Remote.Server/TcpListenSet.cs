namespace PinionCore.Remote.Server
{
    public class TcpListenSet : ListenSet<PinionCore.Remote.Server.Tcp.Listener, Remote.Soul.IService>
    {
        public TcpListenSet(PinionCore.Remote.Server.Tcp.Listener listener, Remote.Soul.IService service) : base(listener, service)
        {

        }
    }
}
