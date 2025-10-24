namespace PinionCore.Remote.Gateway
{
    public class Registry : PinionCore.Remote.Gateway.Registrys.Client
    {
        public Registry(IProtocol protocol, uint group) : base(group,protocol.VersionCode)
        {
        }
    }
}



