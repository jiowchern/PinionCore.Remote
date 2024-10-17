using System;

namespace PinionCore.Serialization
{
    public interface IKeyDescriber
    {
        int GetByteCount(Type type);
        int ToBuffer(Type type, PinionCore.Memorys.Buffer buffer, int begin);
        int ToObject(PinionCore.Memorys.Buffer buffer, int begin, out Type type);
    }
}