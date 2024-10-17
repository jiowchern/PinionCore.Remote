using System;

namespace PinionCore.Serialization
{
    public interface IDescribersFinder
    {
        IKeyDescriber Get();
        ITypeDescriber Get(Type id);
    }
}