namespace PinionCore.Remote
{

    public interface ISessionBinder
    {

        ISoul Return<TSoul>(TSoul soul);

        ISoul Bind<TSoul>(TSoul soul);

        void Unbind(ISoul soul);
    }
}
