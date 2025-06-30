using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace PinionCore.Remote.Tools.Protocol.Sources
{
    public class EssentialReference
    {
        public readonly INamedTypeSymbol PinionCoreRemoteProtocolCreaterAttribute;
        public readonly INamedTypeSymbol PinionCoreRemoteProperty;
        public readonly INamedTypeSymbol PinionCoreRemoteNotifier;
        public readonly INamedTypeSymbol PinionCoreRemoteValue1;
        public readonly INamedTypeSymbol PinionCoreRemoteValue0;
        public readonly INamedTypeSymbol[] SystemActions;
        public readonly Compilation Compilation;

        public readonly System.Func<INamedTypeSymbol, bool> Tag;


        public EssentialReference(Microsoft.CodeAnalysis.Compilation compilation, INamedTypeSymbol tag)
        {

            this.Compilation = compilation;

            PinionCoreRemoteProtocolCreaterAttribute = _GetType("PinionCore.Remote.Protocol.CreaterAttribute");
            PinionCoreRemoteProperty = _GetType("PinionCore.Remote.Property`1");
            PinionCoreRemoteNotifier = _GetType("PinionCore.Remote.Notifier`1");
            PinionCoreRemoteValue1 = _GetType("PinionCore.Remote.Value`1");
            PinionCoreRemoteValue0 = _GetType("PinionCore.Remote.Value");


            SystemActions = new[]
            {
                _GetType("System.Action"),
                _GetType("System.Action`1"),
                _GetType("System.Action`2"),
                _GetType("System.Action`3"),
                _GetType("System.Action`4"),
                _GetType("System.Action`5"),
                _GetType("System.Action`6"),
                _GetType("System.Action`7"),
                _GetType("System.Action`8"),
                _GetType("System.Action`9"),
                _GetType("System.Action`10"),
                _GetType("System.Action`11"),
                _GetType("System.Action`12"),
                _GetType("System.Action`13"),
                _GetType("System.Action`14"),
                _GetType("System.Action`15"),
                _GetType("System.Action`16"),
            };

            Tag = _SetTag(tag);
        }

        private Func<INamedTypeSymbol, bool> _SetTag(INamedTypeSymbol tag)
        {
            if (tag == null)
                return s => true;

            return s => s.AllInterfaces.Any(i => _Equal(tag, i));
        }

        private static bool _Equal(INamedTypeSymbol tag, INamedTypeSymbol i)
        {
            return i.Name == tag.Name;
        }

        INamedTypeSymbol _GetType(string metaname)
        {
            INamedTypeSymbol type = Compilation.GetTypeByMetadataName(metaname);
            if (type == null)
            {
                throw new MissingTypeException(metaname);
            }


            return type;
        }


    }
}
