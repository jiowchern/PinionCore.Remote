namespace PinionCore.Remote.Server
{
    public class WebListenSet : ListenSet<PinionCore.Remote.Server.Web.Listener, Remote.Soul.IService>
    {
        public WebListenSet(PinionCore.Remote.Server.Web.Listener listener, Remote.Soul.IService service) : base(listener, service) { }
    }


}
