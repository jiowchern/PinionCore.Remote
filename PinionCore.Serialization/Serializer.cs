using System;

namespace PinionCore.Serialization
{


    public class Serializer
    {
        private readonly DescriberProvider _Provider;

        readonly Memorys.IPool _Pool;


        public Serializer(DescriberProvider provider) : this(provider, PinionCore.Memorys.PoolProvider.Shared)
        {

        }
        public Serializer(DescriberProvider provider, Memorys.IPool pool)
        {
            _Provider = provider;
            _Pool = pool;
        }

        public PinionCore.Memorys.Buffer ObjectToBuffer(object instance)
        {

            try
            {
                if (instance == null)
                {
                    return _NullBuffer();
                }


                Type type = instance.GetType();
                ITypeDescriber describer = _Provider.TypeDescriberFinders.Get(type);

                var idCount = _Provider.KeyDescriber.GetByteCount(type);
                var bufferCount = describer.GetByteCount(instance);
                Memorys.Buffer buffer = _Pool.Alloc(idCount + bufferCount);
                ArraySegment<byte> bytes = buffer.Bytes;
                var readCount = _Provider.KeyDescriber.ToBuffer(type, buffer, 0);
                describer.ToBuffer(instance, buffer, readCount);
                return buffer;
            }
            catch (DescriberException ex)
            {

                if (instance != null)
                {
                    throw new SystemException(string.Format("ObjectToBuffer {0}", instance.GetType()), ex);
                }
                else
                {
                    throw new SystemException(string.Format("ObjectToBuffer null"), ex);
                }
            }

        }

        private Memorys.Buffer _NullBuffer()
        {
            var idCount = Varint.GetByteCount(0);
            Memorys.Buffer buffer = _Pool.Alloc(idCount);
            ArraySegment<byte> bytes = buffer.Bytes;
            Varint.NumberToBuffer(bytes.Array, bytes.Offset, 0);
            return buffer;
        }

        public object BufferToObject(Memorys.Buffer buffer)
        {
            Type id = null;
            try
            {

                var readIdCount = _Provider.KeyDescriber.ToObject(buffer, 0, out id);
                if (id == null)
                    return null;

                ITypeDescriber describer = _Provider.TypeDescriberFinders.Get(id);
                object instance;
                describer.ToObject(buffer, readIdCount, out instance);
                return instance;
            }
            catch (DescriberException ex)
            {
                ITypeDescriber describer = _Provider.TypeDescriberFinders.Get(id);
                if (describer != null)
                    throw new SystemException(string.Format("BufferToObject {0}:{1}", id, describer.Type.FullName), ex);
                else
                {
                    throw new SystemException(string.Format("BufferToObject {0}:unknown", id), ex);
                }
            }

        }





        public bool TryBufferToObject<T>(PinionCore.Memorys.Buffer buffer, out T pkg)
        {

            pkg = default(T);
            try
            {
                var instance = BufferToObject(buffer);
                pkg = (T)instance;
                return true;
            }
            catch (Exception e)
            {
                PinionCore.Utility.Log.Instance.WriteInfo(e.ToString());
            }

            return false;
        }

        public bool TryBufferToObject(PinionCore.Memorys.Buffer buffer, out object pkg)
        {

            pkg = null;
            try
            {
                var instance = BufferToObject(buffer);
                pkg = instance;
                return true;
            }
            catch (Exception e)
            {
                PinionCore.Utility.Log.Instance.WriteInfo(e.ToString());
            }

            return false;
        }

    }


}



