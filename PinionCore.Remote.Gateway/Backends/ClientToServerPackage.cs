namespace PinionCore.Remote.Gateway.Backends
{
    struct ClientToServerPackage
    {
        public OpCodeClientToServer OpCode;
        public uint Id;
        public byte[] Payload;
    }
    
}
