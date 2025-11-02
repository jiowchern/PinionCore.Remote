using System;

namespace PinionCore.Serialization
{
    public class StringDescriber : ITypeDescriber
    {


        private readonly ITypeDescriber _CharArrayDescriber;
        private readonly System.Text.Encoding _Utf8Encoding;

        public StringDescriber(ITypeDescriber chars_describer)
        {
            _CharArrayDescriber = chars_describer;
            _Utf8Encoding = System.Text.Encoding.UTF8;

        }


        Type ITypeDescriber.Type
        {
            get { return typeof(string); }
        }

        object ITypeDescriber.Default
        {
            get { return null; }
        }

        int ITypeDescriber.GetByteCount(object instance)
        {
            var str = instance as string;

            // Use UTF-8 encoding: Length(varint) + UTF8 bytes
            var utf8ByteCount = _Utf8Encoding.GetByteCount(str);
            var lengthByteCount = Varint.GetByteCount(utf8ByteCount);

            return lengthByteCount + utf8ByteCount;
        }

        int ITypeDescriber.ToBuffer(object instance, PinionCore.Memorys.Buffer buffer, int begin)
        {
            var str = instance as string;
            var offset = begin;

            // Encode string to UTF-8
            var utf8Bytes = _Utf8Encoding.GetBytes(str);
            ArraySegment<byte> bufferBytes = buffer.Bytes;

            // Write length as varint
            offset += Varint.NumberToBuffer(bufferBytes.Array, bufferBytes.Offset + offset, utf8Bytes.Length);

            // Write UTF-8 bytes
            System.Buffer.BlockCopy(utf8Bytes, 0, bufferBytes.Array, bufferBytes.Offset + offset, utf8Bytes.Length);
            offset += utf8Bytes.Length;

            return offset - begin;
        }

        int ITypeDescriber.ToObject(PinionCore.Memorys.Buffer buffer, int begin, out object instance)
        {
            var offset = begin;
            ArraySegment<byte> bufferBytes = buffer.Bytes;

            // Read length as varint
            int utf8ByteCount;
            offset += Varint.BufferToNumber(buffer, offset, out utf8ByteCount);

            // Read UTF-8 bytes and decode to string
            instance = _Utf8Encoding.GetString(bufferBytes.Array, bufferBytes.Offset + offset, utf8ByteCount);
            offset += utf8ByteCount;

            return offset - begin;
        }

    }
}
