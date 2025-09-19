namespace PinionCore.Remote.Gateway
{
    enum OpCodeFromServiceRegistry : byte
    {
        None = 0,
        Join = 1,
        Leave = 2,
        Message = 3,
    }
}
