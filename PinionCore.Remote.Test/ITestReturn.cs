﻿using System;
using PinionCore.Remote;

namespace RemotingTest
{
    public interface ITestInterface
    {
        event Action<int> ReturnEvent;

        Value<int> Add(int a, int b);
    }

    public interface ITestReturn
    {
        Value<ITestInterface> Test(int a, int b);
    }
}
