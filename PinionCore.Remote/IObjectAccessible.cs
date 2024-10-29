﻿namespace PinionCore.Remote
{
    public interface IObjectAccessible
    {
        void Add(object instance);
        void Remove(object instance);
    }
}
