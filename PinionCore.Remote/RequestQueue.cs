namespace PinionCore.Remote
{
    public delegate void InvokeMethodCallback(long entity_id, int method_id, long return_id, byte[][] args);
    public delegate void InvokeStreamMethodCallback(long entity_id, int method_id, long return_id, byte[] buffer, int count);

    public interface IRequestQueue
    {

        event InvokeMethodCallback InvokeMethodEvent;
        event InvokeStreamMethodCallback InvokeStreamMethodEvent;


    }
}
