namespace PinionCore.Remote.Soul
{
    public interface IListenable
    {
        event System.Action<Network.IStreamable> StreamableEnterEvent;
        event System.Action<Network.IStreamable> StreamableLeaveEvent;
    }
}
