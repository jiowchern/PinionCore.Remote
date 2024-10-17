using System;

namespace PinionCore.Serialization
{
    public class StringDescriber : ITypeDescriber
    {


        private readonly ITypeDescriber _CharArrayDescriber;

        public StringDescriber(ITypeDescriber chars_describer)
        {
            _CharArrayDescriber = chars_describer;

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
            string str = instance as string;
            char[] chars = str.ToCharArray();

            int charCount = _CharArrayDescriber.GetByteCount(chars);

            return charCount;
        }

        int ITypeDescriber.ToBuffer(object instance, PinionCore.Memorys.Buffer buffer, int begin)
        {            
            string str = instance as string;
            char[] chars = str.ToCharArray();
            int offset = begin;
            offset += _CharArrayDescriber.ToBuffer(chars, buffer, offset);
            return offset - begin;
        }

        int ITypeDescriber.ToObject(PinionCore.Memorys.Buffer buffer, int begin, out object instnace)
        {
            int offset = begin;
            object chars;
            offset += _CharArrayDescriber.ToObject(buffer, offset, out chars);

            instnace = new string(chars as char[]);

            return offset - begin;
        }

    }
}