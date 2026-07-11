using System;
using System.Linq;

using System.Reflection;

namespace PinionCore.Serialization
{
    public class ClassDescriber : ITypeDescriber
    {


        private readonly Type _Type;

        private readonly FieldInfo[] _Fields;

        private readonly int _MaskLength;

        private readonly object _Default;

        private readonly IDescribersFinder _TypeSet;

        public ClassDescriber(Type type, IDescribersFinder finder)
        {
            _Default = null;
            _Type = type;
            _TypeSet = finder;
            _Fields = (from field in _Type.GetFields()
                       where field.IsStatic == false && field.IsPublic && field.FieldType.IsAbstract == false
                       orderby field.Name
                       select field).ToArray();
            _MaskLength = (_Fields.Length + 7) / 8;
        }

        Type ITypeDescriber.Type
        {
            get { return _Type; }
        }

        public object Default { get { return _Default; } }

        int ITypeDescriber.GetByteCount(object instance)
        {
            try
            {
                var count = _MaskLength;
                for (var i = 0; i < _Fields.Length; i++)
                {
                    FieldInfo field = _Fields[i];
                    var value = field.GetValue(instance);
                    if (object.Equals(_GetDescriber(field).Default, value))
                        continue;

                    if (TypeIdentifier.IsFinal(field.FieldType))
                    {
                        count += _GetDescriber(field).GetByteCount(value);
                    }
                    else
                    {
                        Type valueType = value.GetType();
                        count += _TypeSet.Get().GetByteCount(valueType);
                        count += _TypeSet.Get(valueType).GetByteCount(value);
                    }
                }
                return count;
            }
            catch (Exception ex)
            {
                throw new DescriberException(typeof(ClassDescriber), _Type, "GetByteCount", ex);
            }

        }

        private ITypeDescriber _GetDescriber(FieldInfo field)
        {
            return _TypeSet.Get(field.FieldType);
        }

        int ITypeDescriber.ToBuffer(object instance, PinionCore.Memorys.Buffer buffer, int begin)
        {

            try
            {
                ArraySegment<byte> bytes = buffer.Bytes;
                var offset = begin;

                var maskOffset = bytes.Offset + offset;
                for (var m = 0; m < _MaskLength; m++)
                {
                    bytes.Array[maskOffset + m] = 0;
                }
                offset += _MaskLength;

                for (var i = 0; i < _Fields.Length; i++)
                {
                    FieldInfo field = _Fields[i];
                    var value = field.GetValue(instance);
                    if (object.Equals(_GetDescriber(field).Default, value))
                        continue;

                    bytes.Array[maskOffset + i / 8] |= (byte)(1 << (i % 8));

                    ITypeDescriber describer;
                    if (TypeIdentifier.IsFinal(field.FieldType))
                    {
                        describer = _GetDescriber(field);
                    }
                    else
                    {
                        Type valueType = value.GetType();
                        offset += _TypeSet.Get().ToBuffer(valueType, buffer, offset);
                        describer = _TypeSet.Get(valueType);
                    }
                    offset += describer.ToBuffer(value, buffer, offset);
                }

                return offset - begin;
            }
            catch (Exception ex)
            {

                throw new DescriberException(typeof(ClassDescriber), _Type, "ToBuffer", ex);
            }

        }

        int ITypeDescriber.ToObject(PinionCore.Memorys.Buffer buffer, int begin, out object instance)
        {
            try
            {
                instance = _CreateInstance();

                ArraySegment<byte> bytes = buffer.Bytes;
                var offset = begin;
                var maskOffset = bytes.Offset + offset;
                offset += _MaskLength;

                for (var i = 0; i < _Fields.Length; i++)
                {
                    if ((bytes.Array[maskOffset + i / 8] & (1 << (i % 8))) == 0)
                        continue;

                    FieldInfo field = _Fields[i];
                    ITypeDescriber describer;
                    if (TypeIdentifier.IsFinal(field.FieldType))
                    {
                        describer = _GetDescriber(field);
                    }
                    else
                    {
                        Type valueType;
                        offset += _TypeSet.Get().ToObject(buffer, offset, out valueType);
                        describer = _TypeSet.Get(valueType);
                    }

                    object valueInstance;
                    offset += describer.ToObject(buffer, offset, out valueInstance);
                    field.SetValue(instance, valueInstance);
                }

                return offset - begin;

            }
            catch (Exception ex)
            {

                throw new DescriberException(typeof(ClassDescriber), _Type, "ToObject", ex);
            }


        }

        private object _CreateInstance()
        {
            object instance;
            ConstructorInfo constructor = _Type.GetConstructors().OrderBy(c => c.GetParameters().Length).Select(c => c).FirstOrDefault();
            if (constructor != null)
            {
                Type[] argTypes = constructor.GetParameters().Select(info => info.ParameterType).ToArray();
                var objArgs = new object[argTypes.Length];

                for (var i = 0; i < argTypes.Length; i++)
                {
                    objArgs[i] = Activator.CreateInstance(argTypes[i]);
                }
                instance = Activator.CreateInstance(_Type, objArgs);
            }
            else
            {
                instance = Activator.CreateInstance(_Type);
            }

            return instance;
        }

    }
}
