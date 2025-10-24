using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PinionCore.Remote.Tools.Protocol.Sources
{
    using System.Linq;



    class SerializableExtractor
    {
        public string Code;



        public SerializableExtractor(Compilation compilation, IEnumerable<TypeSyntax> types)
        {
            var serializable = new System.Collections.Generic.List<ITypeSymbol>();
            TypeSyntax[] ttt = types.ToArray();
            foreach (TypeSyntax t in types)
            {
                TypeSyntax type = t;
                var typeName = type.ToFullString();
                INamedTypeSymbol typeSymbol = compilation.GetTypeByMetadataName(typeName);

                _Collect(typeSymbol, serializable);

                if (type is ArrayTypeSyntax arrayType)
                {
                    TypeSyntax elementType = arrayType.ElementType;
                    var elementTypeName = elementType.ToFullString();
                    INamedTypeSymbol elementTypeSymbol = compilation.GetTypeByMetadataName(elementTypeName);
                    if (elementTypeSymbol == null)
                    {
                        continue;
                    }
                    _Collect(elementTypeSymbol, serializable);
                    IArrayTypeSymbol arraySymbol = compilation.CreateArrayTypeSymbol(elementTypeSymbol);
                    _Collect(arraySymbol, serializable);
                }
            }
            var typeNames = serializable
                .Select(type => type.ToDisplayString())
                .Distinct(System.StringComparer.Ordinal)
                .OrderBy(name => name, System.StringComparer.Ordinal);

            Code = string.Join(",", typeNames.Select(name => $"typeof({name})"));
        }

        private void _Collect(ITypeSymbol typeSymbol, List<ITypeSymbol> serializables)
        {
            if (typeSymbol == null)
                return;
            if (serializables.Contains(typeSymbol))
                return;
            serializables.Add(typeSymbol);

            if (typeSymbol is IArrayTypeSymbol arraySymbol)
            {
                _Collect(arraySymbol.ElementType, serializables);
            }

            IEnumerable<IFieldSymbol> fields = typeSymbol.GetMembers().OfType<IFieldSymbol>().Where(f => f.DeclaredAccessibility == Accessibility.Public);
            foreach (IFieldSymbol field in fields)
            {
                _Collect(field.Type, serializables);
            }
        }

    }
}
