using System;


namespace PinionCore.Remote.Tests
{
    class Serializer : ISerializable
    {

        private readonly Serialization.Dynamic.Serializer _Serializer;
        public readonly ISerializable Serializable;
        public Serializer()
        {
            _Serializer = new PinionCore.Serialization.Dynamic.Serializer();
            Serializable = this;
        }
        object ISerializable.Deserialize(Type type, PinionCore.Memorys.Buffer buffer)
        {
            return _Serializer.BufferToObject(buffer);
        }

        PinionCore.Memorys.Buffer ISerializable.Serialize(Type type, object instance)
        {
            return _Serializer.ObjectToBuffer(instance);
        }
    }
}
