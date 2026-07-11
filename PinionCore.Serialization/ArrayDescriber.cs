using System;
using System.Collections;
using System.Collections.Generic;

namespace PinionCore.Serialization
{
    public class ArrayDescriber : ITypeDescriber
    {

        private readonly Type _Type;


        private readonly object _Default;
        private readonly object _DefaultElement;
        private readonly Type _ElementType;
        private readonly bool _ElementFinal;
        private readonly IDescribersFinder _TypeSet;

        public ArrayDescriber(Type type, IDescribersFinder finder)
        {

            _TypeSet = finder;
            _Default = null;
            _Type = type;
            Type elementType = type.GetElementType();
            _ElementType = elementType;
            _ElementFinal = TypeIdentifier.IsFinal(elementType);
            try
            {
                if (!elementType.IsClass)
                    _DefaultElement = Activator.CreateInstance(elementType);
                else
                {
                    _DefaultElement = null;
                }

            }
            catch (Exception ex)
            {

                throw new DescriberException(typeof(ArrayDescriber), _Type, "_DefaultElement", ex);
            }

        }



        Type ITypeDescriber.Type
        {
            get { return _Type; }
        }

        public object Default { get { return _Default; } }


        struct ValidObject
        {
            public int Index;
            public object Object;
        }


        struct ValidObjectSet
        {
            public int TotalLength;
            public int ValidLength;

            public ValidObject[] ValidObjects;
        }

        private ValidObjectSet _GetSet(object instance)
        {
            var array = instance as IList;

            var validLength = 0;
            for (var i = 0; i < array.Count; i++)
            {
                var obj = array[i];
                if (object.Equals(obj, _DefaultElement) == false)
                {
                    validLength++;
                }
            }



            var validObjects = new List<ValidObject>();
            for (var i = 0; i < array.Count; i++)
            {
                var obj = array[i];
                var index = i;
                if (object.Equals(obj, _DefaultElement) == false)
                {

                    validObjects.Add(new ValidObject() { Index = index, Object = obj });
                }
            }

            return new ValidObjectSet()
            {
                TotalLength = array.Count,
                ValidLength = validLength,
                ValidObjects = validObjects.ToArray()
            };
        }
        int ITypeDescriber.GetByteCount(object instance)
        {
            ValidObjectSet set = _GetSet(instance);


            var lenCount = Varint.GetByteCount(set.TotalLength);
            var validCount = Varint.GetByteCount(set.ValidLength);

            var dense = set.ValidLength == set.TotalLength;

            var instanceCount = 0;
            for (var i = 0; i < set.ValidObjects.Length; i++)
            {
                var index = set.ValidObjects[i].Index;
                var obj = set.ValidObjects[i].Object;

                if (!dense)
                    instanceCount += Varint.GetByteCount(index);

                ITypeDescriber describer;
                if (_ElementFinal)
                {
                    describer = _TypeSet.Get(_ElementType);
                }
                else
                {
                    Type objType = obj.GetType();
                    instanceCount += _TypeSet.Get().GetByteCount(objType);
                    describer = _TypeSet.Get(objType);
                }
                instanceCount += describer.GetByteCount(obj);
            }

            return instanceCount + lenCount + validCount;
        }

        int ITypeDescriber.ToBuffer(object instance, PinionCore.Memorys.Buffer buffer, int begin)
        {

            try
            {
                ArraySegment<byte> bytes = buffer.Bytes;
                ValidObjectSet set = _GetSet(instance);
                var offset = begin;
                offset += Varint.NumberToBuffer(bytes.Array, bytes.Offset + offset, set.TotalLength);
                offset += Varint.NumberToBuffer(bytes.Array, bytes.Offset + offset, set.ValidLength);

                var dense = set.ValidLength == set.TotalLength;

                for (var i = 0; i < set.ValidObjects.Length; i++)
                {
                    var index = set.ValidObjects[i].Index;
                    var obj = set.ValidObjects[i].Object;

                    if (!dense)
                        offset += Varint.NumberToBuffer(bytes.Array, bytes.Offset + offset, index);

                    ITypeDescriber describer;
                    if (_ElementFinal)
                    {
                        describer = _TypeSet.Get(_ElementType);
                    }
                    else
                    {
                        Type objType = obj.GetType();
                        offset += _TypeSet.Get().ToBuffer(objType, buffer, offset);
                        describer = _TypeSet.Get(objType);
                    }
                    offset += describer.ToBuffer(obj, buffer, offset);
                }

                return offset - begin;
            }
            catch (Exception ex)
            {

                throw new DescriberException(typeof(ArrayDescriber), _Type, "ToBuffer", ex);
            }

        }

        int ITypeDescriber.ToObject(PinionCore.Memorys.Buffer buffer, int begin, out object instance)
        {
            try
            {
                var offset = begin;
                ulong count;
                offset += Varint.BufferToNumber(buffer, offset, out count);
                var array = Activator.CreateInstance(_Type, (int)count) as IList;
                instance = array;

                ulong validCount;
                offset += Varint.BufferToNumber(buffer, offset, out validCount);

                var dense = validCount == count;

                for (var i = 0UL; i < validCount; i++)
                {
                    var index = i;
                    if (!dense)
                    {
                        offset += Varint.BufferToNumber(buffer, offset, out index);
                    }

                    ITypeDescriber describer;
                    if (_ElementFinal)
                    {
                        describer = _TypeSet.Get(_ElementType);
                    }
                    else
                    {
                        Type objType;
                        offset += _TypeSet.Get().ToObject(buffer, offset, out objType);
                        describer = _TypeSet.Get(objType);
                    }
                    object value;
                    offset += describer.ToObject(buffer, offset, out value);


                    array[(int)index] = value;
                }

                return offset - begin;
            }
            catch (Exception ex)
            {

                throw new DescriberException(typeof(ArrayDescriber), _Type, "ToObject", ex); ;
            }

        }




    }
}
