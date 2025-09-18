using System;

namespace PinionCore.Serialization
{
    public class ByteArrayDescriber : ITypeDescriber
    {

        private readonly ITypeDescriber _IntTypeDescriber;

        public ByteArrayDescriber(ITypeDescriber byte_describer)
        {
            _IntTypeDescriber = byte_describer;
        }


        Type ITypeDescriber.Type
        {
            get { return typeof(byte[]); }
        }

        object ITypeDescriber.Default
        {
            get { return null; }
        }

        int ITypeDescriber.GetByteCount(object instance)
        {
            var array = instance as Array;
            var len = array.Length;
            var lenByetCount = _IntTypeDescriber.GetByteCount(len);
            return len + lenByetCount;
        }

        int ITypeDescriber.ToBuffer(object instance, PinionCore.Memorys.Buffer buffer, int begin)
        {
            var array = instance as byte[];
            var len = array.Length;

            var offset = begin;


            offset += _IntTypeDescriber.ToBuffer(len, buffer, offset);
            for (var i = 0; i < len; i++)
            {
                buffer[offset++] = array[i];
            }
            return offset - begin;
        }

        int ITypeDescriber.ToObject(PinionCore.Memorys.Buffer buffer, int begin, out object instance)
        {
            var offset = begin;
            object lenObject = null;
            offset += _IntTypeDescriber.ToObject(buffer, offset, out lenObject);

            var len = (int)lenObject;
            var array = new byte[len];
            for (var i = 0; i < len; i++)
            {
                array[i] = buffer[offset++];
            }
            instance = array;
            return offset - begin;
        }


    }
}
