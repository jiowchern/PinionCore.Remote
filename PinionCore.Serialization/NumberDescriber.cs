using System;
using System.Runtime.InteropServices;

namespace PinionCore.Serialization
{


    public class NumberDescriber<T> : NumberDescriber
    {
        public NumberDescriber() : base(typeof(T)) { }
    }
    public class NumberDescriber : ITypeDescriber
    {

        private readonly Type _Type;
        private readonly object _Default;
        private readonly int _Size;


        public NumberDescriber(Type type)
        {

            var t = new System.Tuple<int>(0);
            _Default = Activator.CreateInstance(type);

            _Size = Marshal.SizeOf(type);


            _Type = type;
        }



        public Type Type { get { return _Type; } }

        int ITypeDescriber.GetByteCount(object instance)
        {
            var instanceVal = _GetUInt64WithZigZag(instance);
            return Varint.GetByteCount(instanceVal);
        }
        public object Default { get { return _Default; } }
        int ITypeDescriber.ToBuffer(object instance, PinionCore.Memorys.Buffer buffer, int begin)
        {
            ArraySegment<byte> bytes = buffer.Bytes;
            var instanceVal = _GetUInt64WithZigZag(instance);
            return Varint.NumberToBuffer(bytes.Array, bytes.Offset + begin, instanceVal);
        }


        private ulong _GetUInt64(object instance)
        {
            var bytes = new byte[_Size];

            IntPtr ptr = Marshal.AllocHGlobal(_Size);

            Marshal.StructureToPtr(instance, ptr, false);

            Marshal.Copy(ptr, bytes, 0, _Size);

            Marshal.FreeHGlobal(ptr);

            var val = 0ul;
            for (var i = 0; i < _Size; i++)
            {
                var b = (ulong)bytes[i];
                b <<= i * 8;
                val |= b;
            }
            return val;
        }

        private ulong _GetUInt64WithZigZag(object instance)
        {
            // Apply ZigZag encoding for signed types
            if (Type == typeof(int))
            {
                return ZigZag.Encode((int)instance);
            }
            else if (Type == typeof(long))
            {
                return ZigZag.Encode((long)instance);
            }
            else if (Type == typeof(short))
            {
                return ZigZag.Encode((int)(short)instance);
            }
            else
            {
                // For unsigned types and others, use raw conversion
                return _GetUInt64(instance);
            }
        }


        int ITypeDescriber.ToObject(PinionCore.Memorys.Buffer buffer, int begin, out object instance)
        {
            ulong value;
            var bytesRead = Varint.BufferToNumber(buffer, begin, out value);

            if (Type == typeof(byte))
            {
                instance = (byte)value;
            }
            else if (Type == typeof(short))
            {
                // Decode ZigZag for signed short
                instance = (short)ZigZag.Decode((uint)value);
            }
            else if (Type == typeof(ushort))
            {
                instance = (ushort)value;
            }
            else if (Type == typeof(int))
            {
                // Decode ZigZag for signed int
                instance = ZigZag.Decode((uint)value);
            }
            else if (Type == typeof(uint))
            {
                instance = (uint)value;
            }
            else if (Type == typeof(long))
            {
                // Decode ZigZag for signed long
                instance = ZigZag.Decode(value);
            }
            else if (Type == typeof(char))
            {
                instance = (char)value;
            }
            else if (Type == typeof(bool))
            {
                instance = value != 0;
            }
            else
            {
                instance = value;
            }


            return bytesRead;
        }


        private static T _Cast<T>(object o)
        {
            return (T)o;
        }


    }
}
