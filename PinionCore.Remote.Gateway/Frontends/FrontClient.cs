using PinionCore.Network;
using PinionCore.Remote.Ghost;

namespace PinionCore.Remote.Gateway.Frontends
{

    public class FrontClient
    {

        public void Launch(IStreamable streamable)
        {
            throw new System.NotImplementedException();
        }
        public void Shutdown(IStreamable streamable)
        {
            throw new System.NotImplementedException();
        }

        public event System.Action<uint, IStreamable> OnServiceConnected;
        public event System.Action<uint, IStreamable> OnServiceDisconnected;
    }
}
