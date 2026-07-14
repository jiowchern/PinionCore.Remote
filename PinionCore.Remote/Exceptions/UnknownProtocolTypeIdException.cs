namespace PinionCore.Remote.Exceptions
{
    public class UnknownProtocolTypeIdException : System.Exception
    {
        public readonly int TypeId;

        public UnknownProtocolTypeIdException(int typeId)
            : base($"Received soul with unknown protocol type id {typeId}: no Spirit interface is registered for this id in the client protocol. Check that server and client use the same protocol version, and that the Spirit interface inherits PinionCore.Remote.Protocolable on both sides.")
        {
            TypeId = typeId;
        }
    }
}
