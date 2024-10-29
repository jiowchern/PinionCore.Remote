﻿using System;
using PinionCore.Network;

namespace PinionCore.Remote.Ghost
{
    public interface IAgent : INotifierQueryable
    {
        bool Active { get; }

        float Ping { get; }

        event Action<string, string> ErrorMethodEvent;
        event Action<System.Exception> ExceptionEvent;

        void Update();
        void Enable(IStreamable streamable);
        void Disable();
    }
}
