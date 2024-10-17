using System;

namespace PinionCore.Remote
{

    public interface IBinder
    {        

        ISoul Return<TSoul>(TSoul soul);

        ISoul Bind<TSoul>(TSoul soul);

        void Unbind(ISoul soul);
    }
}
