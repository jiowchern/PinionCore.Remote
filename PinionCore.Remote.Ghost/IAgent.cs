using System;
using PinionCore.Network;

namespace PinionCore.Remote.Ghost
{
    public interface IAgent : INotifierQueryable
    {        

        float Ping { get; }

        event Action<byte[], byte[]> VersionCodeErrorEvent;
        event Action<string, string> ErrorMethodEvent;
        event Action<System.Exception> ExceptionEvent;

        void HandlePackets();
        void HandleMessage();
        void Enable(IStreamable streamable);
        void Disable();
    }
}
