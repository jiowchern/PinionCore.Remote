using PinionCore.Network;

namespace PinionCore.Remote.Gateway.Protocols
{    
    public interface IConnection : IStreamable
    {

    }
    public interface IStreamProviable
    {
        void Exit();
        PinionCore.Remote.Notifier<IStreamable> Streams { get; }
    }
}

