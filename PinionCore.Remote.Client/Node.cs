namespace PinionCore.Remote.Client
{
    public class Node 
    {
        public readonly Ghost.IAgent User;
        public Node(IProtocol protocol)            
        {
            User = new Ghost.User(protocol);
        }
    }
}
