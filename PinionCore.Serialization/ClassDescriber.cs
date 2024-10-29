﻿using System;
using System.Linq;

using System.Reflection;

namespace PinionCore.Serialization
{
    public class ClassDescriber : ITypeDescriber
    {


        private readonly Type _Type;

        private readonly FieldInfo[] _Fields;

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
                var validFields = _Fields.Select(
                       (field, index) => new
                       {
                           field,
                           index
                       }).Where(validField => object.Equals(_GetDescriber(validField.field).Default, validField.field.GetValue(instance)) == false).ToArray();


                var validCount = Varint.GetByteCount(validFields.Length);
                var count = 0;
                for (var i = 0; i < validFields.Length; i++)
                {

                    var validField = validFields[i];
                    var index = validField.index;
                    FieldInfo field = validField.field;

                    count = _GetCount(instance, count, index, field);
                }
                return count + validCount;
            }
            catch (Exception ex)
            {
                throw new DescriberException(typeof(ClassDescriber), _Type, "GetByteCount", ex);
            }

        }

        private int _GetCount(object instance, int count, int index, FieldInfo field)
        {
            var value = field.GetValue(instance);
            Type valueType = value.GetType();
            var valueTypeCount = _TypeSet.Get().GetByteCount(valueType);
            ITypeDescriber describer = _TypeSet.Get(valueType);
            var byteCount = describer.GetByteCount(value);
            var indexCount = Varint.GetByteCount(index);
            count += byteCount + indexCount + valueTypeCount;
            return count;
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
                var validFields = _Fields.Select(
                           (field, index) => new
                           {
                               field,
                               index
                           }).Where(validField => object.Equals(_GetDescriber(validField.field).Default, validField.field.GetValue(instance)) == false)
                       .ToArray();

                offset += Varint.NumberToBuffer(bytes.Array, bytes.Offset + offset, validFields.Length);


                foreach (var validField in validFields)
                {
                    var index = validField.index;
                    FieldInfo field = validField.field;

                    offset = _ToBuffer(instance, buffer, offset, index, field);
                }


                return offset - begin;
            }
            catch (Exception ex)
            {

                throw new DescriberException(typeof(ClassDescriber), _Type, "ToBuffer", ex);
            }

        }

        private int _ToBuffer(object instance, Memorys.Buffer buffer, int offset, int index, FieldInfo field)
        {
            ArraySegment<byte> bytes = buffer.Bytes;
            offset += Varint.NumberToBuffer(bytes.Array, bytes.Offset + offset, index);
            var value = field.GetValue(instance);
            Type valueType = value.GetType();
            offset += _TypeSet.Get().ToBuffer(valueType, buffer, offset);
            ITypeDescriber describer = _TypeSet.Get(valueType);
            offset += describer.ToBuffer(value, buffer, offset);
            return offset;
        }

        int ITypeDescriber.ToObject(PinionCore.Memorys.Buffer buffer, int begin, out object instance)
        {
            try
            {
                instance = _CreateInstance();

                var offset = begin;

                ulong validLength;
                offset += Varint.BufferToNumber(buffer, offset, out validLength);

                for (var i = 0ul; i < validLength; i++)
                {
                    offset = _ToObject(buffer, instance, offset);
                }

                return offset - begin;

            }
            catch (Exception ex)
            {

                throw new DescriberException(typeof(ClassDescriber), _Type, "ToObject", ex);
            }


        }

        private int _ToObject(PinionCore.Memorys.Buffer buffer, object instance, int offset)
        {
            ulong index;
            offset += Varint.BufferToNumber(buffer, offset, out index);
            Type valueType;
            offset += _TypeSet.Get().ToObject(buffer, offset, out valueType);
            FieldInfo filed = _Fields[index];
            ITypeDescriber describer = _TypeSet.Get(valueType);
            object valueInstance;
            offset += describer.ToObject(buffer, offset, out valueInstance);
            filed.SetValue(instance, valueInstance);
            return offset;
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
