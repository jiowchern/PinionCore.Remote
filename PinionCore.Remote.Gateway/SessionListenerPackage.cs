namespace PinionCore.Remote.Gateway
{
    struct SessionListenerPackage
    {
        public OpCodeFromSessionListener OpCode;
        public uint UserId;
        public byte[] Payload;
    }
}
