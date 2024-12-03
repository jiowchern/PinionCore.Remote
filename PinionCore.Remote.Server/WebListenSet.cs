namespace PinionCore.Remote.Server
{
    public class WebListenSet : ListenSet<PinionCore.Remote.Server.Web.Listener, Soul.IService>
    {
        public WebListenSet(PinionCore.Remote.Server.Web.Listener listener, Soul.IService service) : base(listener, service) { }
    }


}
