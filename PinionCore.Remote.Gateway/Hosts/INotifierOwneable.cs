namespace PinionCore.Remote.Gateway.Hosts
{
    interface INotifierOwneable
    {
        void AddNotifier(INotifierQueryable notifierQueryable);
        void RemoveNotifier(INotifierQueryable notifierQueryable);
    }
}



