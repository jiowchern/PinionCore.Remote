namespace PinionCore.Consoles.Chat1
{
    interface IMessageable
    {
        string Name { get; }

        void PublicReceive(Common.Message msg);
        void PrivateReceive(Common.Message msg);
    }
}
