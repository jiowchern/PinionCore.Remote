using System;

namespace PinionCore.Serialization
{
    public class BufferDescriber<T> : BufferDescriber
    {
        public BufferDescriber() : base(typeof(T))
        {

        }
    }
    public class BufferDescriber : ITypeDescriber
    {
        private readonly Type _Type;


        public BufferDescriber(Type type)
        {
            if (type.IsArray == false)
                throw new ArgumentException("type is not array " + type.FullName);
            _Type = type;

        }



        Type ITypeDescriber.Type
        {
            get { return _Type; }
        }

        object ITypeDescriber.Default
        {
            get { return null; }
        }

        int ITypeDescriber.GetByteCount(object instance)
        {
            var src = instance as Array;
            var bufferLength = Buffer.ByteLength(src);
            var bufferLen = Varint.GetByteCount(bufferLength);
            var elementLen = Varint.GetByteCount(src.Length);
            return bufferLen + bufferLength + elementLen;
        }

        int ITypeDescriber.ToBuffer(object instance, PinionCore.Memorys.Buffer buffer, int begin)
        {
            ArraySegment<byte> bytes = buffer.Bytes;
            var src = instance as Array;
            var bufferLength = Buffer.ByteLength(src);
            var offset = begin;
            offset += Varint.NumberToBuffer(bytes.Array, bytes.Offset + offset, bufferLength);
            offset += Varint.NumberToBuffer(bytes.Array, bytes.Offset + offset, src.Length);



            Buffer.BlockCopy(src, 0, bytes.Array, bytes.Offset + offset, bufferLength);
            return offset - begin + bufferLength;
        }

        int ITypeDescriber.ToObject(PinionCore.Memorys.Buffer buffer, int begin, out object instnace)
        {
            var offset = begin;
            var bufferLen = 0;
            offset += Varint.BufferToNumber(buffer, offset, out bufferLen);
            var elementLen = 0;
            offset += Varint.BufferToNumber(buffer, offset, out elementLen);
            Array dst = _Create(elementLen);

            ArraySegment<byte> bytes = buffer.Bytes;
            Buffer.BlockCopy(bytes.Array, bytes.Offset + offset, dst, 0, bufferLen);

            instnace = dst;
            return offset - begin + bufferLen;
        }

        private Array _Create(int len)
        {
            return Activator.CreateInstance(_Type, new object[] { len }) as Array;
        }


    }
}
