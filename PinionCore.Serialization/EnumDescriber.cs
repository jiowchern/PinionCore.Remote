using System;

namespace PinionCore.Serialization
{

    public class EnumDescriber<T> : EnumDescriber
    {
        public EnumDescriber() : base(typeof(T))
        {

        }
    }
    public class EnumDescriber : ITypeDescriber
    {


        private readonly Type _Type;

        private readonly object _Default;

        public EnumDescriber(Type type)
        {

            _Type = type;

            _Default = Activator.CreateInstance(type);
        }



        Type ITypeDescriber.Type
        {
            get { return _Type; }
        }

        object ITypeDescriber.Default
        {
            get { return _Default; }
        }

        int ITypeDescriber.GetByteCount(object instance)
        {
            return Varint.GetByteCount(Convert.ToUInt64(instance));
        }

        int ITypeDescriber.ToBuffer(object instance, PinionCore.Memorys.Buffer buffer, int begin)
        {
            ArraySegment<byte> bytes = buffer.Bytes;
            return Varint.NumberToBuffer(bytes.Array, bytes.Offset + begin, Convert.ToUInt64(instance));
        }

        int ITypeDescriber.ToObject(PinionCore.Memorys.Buffer buffer, int begin, out object instance)
        {
            ulong value;
            var bytesRead = Varint.BufferToNumber(buffer, begin, out value);

            instance = Enum.ToObject(_Type, value);
            return bytesRead;
        }


    }
}
