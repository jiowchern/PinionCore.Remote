using System;

namespace PinionCore.Remote
{
    public class DynamicSerializer : ISerializable
    {
        private readonly Serialization.Dynamic.Serializer _Serializer;

        public DynamicSerializer()
        {
            _Serializer = new PinionCore.Serialization.Dynamic.Serializer();
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
