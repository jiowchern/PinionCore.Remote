using System;
using System.Collections.Generic;
using System.Linq;
using PinionCore.Memorys;

namespace PinionCore.Remote.Gateway
{
    public class Serializer
    {
        readonly PinionCore.Serialization.Serializer _Serializer;
        readonly IPool _Pool;
        public Serializer(IPool pool ,IEnumerable<Type> essentialTypes)
        {
            _Pool = pool;
            /*var essentialTypes = new System.Type[]
            {
                typeof(Package),
                typeof(uint),
                typeof(byte[]),
                typeof(byte)
            };*/
            _Serializer = new PinionCore.Serialization.Serializer(new PinionCore.Serialization.DescriberBuilder(essentialTypes.ToArray()).Describers , pool);
        }

        public PinionCore.Memorys.Buffer Serialize(object package)
        {
            var buffer = _Serializer.ObjectToBuffer(package);
            
            return buffer;
        }
        public object Deserialize(PinionCore.Memorys.Buffer buffer)
        {            
            return _Serializer.BufferToObject(buffer);
        }

    }
}
