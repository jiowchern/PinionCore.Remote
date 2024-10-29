using System;
using System.Linq;
namespace PinionCore.Remote
{

    public class Serializer : ISerializable
    {
        private readonly Serialization.Serializer _Serializer;

        public Serializer(System.Collections.Generic.IEnumerable<System.Type> types)
        {
            _Serializer = new PinionCore.Serialization.Serializer(new PinionCore.Serialization.DescriberBuilder(types.ToArray()).Describers);
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
