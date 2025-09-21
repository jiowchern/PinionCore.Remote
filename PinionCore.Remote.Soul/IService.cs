using System;
using PinionCore.Network;

namespace PinionCore.Remote.Soul
{
    public interface IService : IDisposable
    {
        void Join(IStreamable user);
        void Leave(IStreamable user);
    }
}
