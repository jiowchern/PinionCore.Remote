using System;
using PinionCore.Memorys;

namespace PinionCore.Remote.Gateway
{
    class ServiceRegistrySessionListenerSerializer : Serializer
    {
        public ServiceRegistrySessionListenerSerializer(IPool pool) : base(pool, new Type[] {
            typeof(ServiceRegistryPackage),
            typeof(OpCodeFromServiceRegistry),
            typeof(SessionListenerPackage),
            typeof(OpCodeFromSessionListener),
            typeof(byte[]),
            typeof(uint),
            typeof(byte)
        })
        {
        }
    }
}

