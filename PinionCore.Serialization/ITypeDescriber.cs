using System;

namespace PinionCore.Serialization
{
    public interface ITypeDescriber
    {

        Type Type { get; }

        object Default { get; }

        int GetByteCount(object instance);
        int ToBuffer(object instance,PinionCore.Memorys.Buffer buffer, int begin);
        int ToObject(PinionCore.Memorys.Buffer buffer, int begin, out object instnace);
    }
}