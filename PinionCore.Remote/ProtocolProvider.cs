using System.Linq;

namespace PinionCore.Remote.Protocol
{
    public static class ProtocolProvider
    {
        public static System.Collections.Generic.IEnumerable<PinionCore.Remote.IProtocol> Create(System.Reflection.Assembly protocol_assembly)
        {
            System.Type[] types = protocol_assembly.GetExportedTypes();
            var protocolTypes = types.Where(type => type.GetInterface(nameof(PinionCore.Remote.IProtocol)) != null);
            foreach (var type in protocolTypes)
            {
                yield return System.Activator.CreateInstance(type) as PinionCore.Remote.IProtocol;
            }
            
        }

        public static System.Collections.Generic.IEnumerable<System.Type> GetProtocols()
        {
            return System.AppDomain.CurrentDomain.GetAssemblies().Where(a => a.IsDynamic == false).SelectMany(assembly => assembly.GetExportedTypes().Where(type => typeof(PinionCore.Remote.IProtocol).IsAssignableFrom(type) && typeof(PinionCore.Remote.IProtocol) != type).Select(t => t));
        }


    }
}
