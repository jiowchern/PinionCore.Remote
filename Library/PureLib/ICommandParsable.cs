﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Regulus.Framework
{
    public interface ICommandParsable<T>
    {

        void Setup();
        void Clear();
    }
}