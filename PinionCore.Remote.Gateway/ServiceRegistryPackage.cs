namespace PinionCore.Remote.Gateway
{
    struct ServiceRegistryPackage
    {
        public OpCodeFromServiceRegistry OpCode;
        public uint UserId;
        public byte[] Payload;
    }
}
