namespace PinionCore.Remote
{
    public class SupplyDepot<T>  where T : class
    {
        public readonly Notifier<T> Supplier;
        public readonly Depot<T> Depot;

        public SupplyDepot()
        {
            Depot = new Depot<T>();
            Supplier = new Notifier<T>(Depot);
        }
    }
}
