using System;

namespace PinionCore.Remote
{
    public delegate void InvokeMethodCallback(long entity_id, int method_id, long return_id, byte[][] args);

    public interface IRequestQueue
    {        

        event InvokeMethodCallback InvokeMethodEvent;


    }
}
