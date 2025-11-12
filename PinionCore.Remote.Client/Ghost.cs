namespace PinionCore.Remote.Client
{
    public class Ghost 
    {
        public readonly Remote.Ghost.IAgent User;
        public Ghost(IProtocol protocol)            
        {
            User = new Remote.Ghost.User(protocol);
        }
    }
}
