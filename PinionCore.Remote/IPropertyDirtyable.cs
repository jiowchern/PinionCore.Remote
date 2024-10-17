using System;

namespace PinionCore.Remote
{
    public interface IDirtyable
    {
        event Action<object> ChangeEvent;
    }
}