namespace PinionCore.Remote.Gateway.Sessions
{
    enum OpCodeClientToServer : byte
    {
        None = 0,
        Join = 1,
        Leave = 2,
        Message = 3
    }
    
}
