namespace PinionCore.Remote.Gateway.Backends
{
    struct ServerToClientPackage
    {
        public OpCodeServerToClient OpCode;
        public uint Id;
        public byte[] Payload;
    }
}
