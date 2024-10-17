using System;

namespace PinionCore.Serialization.Dynamic
{
    public interface ITypeFinder
    {
        Type Find(string type);
    }
}