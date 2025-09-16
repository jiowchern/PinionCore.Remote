namespace PinionCore.Remote.Gateway.Sessions
{
    struct ClientToServerPackage
    {
        public OpCodeClientToServer OpCode;
        public uint Id;
        public byte[] Payload;
    }
    
}
