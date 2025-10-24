
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PinionCore.Remote.Tools.Protocol.Sources
{
    public static class CompilationExtensions
    {
        public static System.Collections.Generic.IEnumerable<INamedTypeSymbol> FindAllInterfaceSymbol(this Compilation com)
        {
            return com.FindAllInterfaceSymbol(s => true);
        }
        public static System.Collections.Generic.IEnumerable<INamedTypeSymbol> FindAllInterfaceSymbol(this Compilation com, System.Func<INamedTypeSymbol, bool> additional)
        {
            System.Collections.Generic.IEnumerable<INamedTypeSymbol> symbols = (from syntaxTree in com.SyntaxTrees
                                                                                let model = com.GetSemanticModel(syntaxTree)
                                                                                from interfaneSyntax in syntaxTree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>()
                                                                                let symbol = model.GetDeclaredSymbol(interfaneSyntax)
                                                                                where symbol.IsGenericType == false && symbol.IsAbstract && additional(symbol)
                                                                                select symbol).SelectMany(s => s.AllInterfaces.Concat(new[] { s }));

            var unique = new System.Collections.Generic.HashSet<INamedTypeSymbol>(symbols, SymbolEqualityComparer.Default);
            return unique.OrderBy(symbol => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), System.StringComparer.Ordinal);
        }

        public static bool AllGhostable(this Compilation compilation, System.Collections.Generic.IEnumerable<TypeSyntax> types)
        {
            foreach (TypeSyntax t in types)
            {
                INamedTypeSymbol typeSymbol = compilation.GetTypeByMetadataName(t.ToString());
                if (typeSymbol == null)
                    return false;
                if (typeSymbol.TypeKind != TypeKind.Interface)
                    return false;
            }
            return true;
        }

        public static bool AllSerializable(this Compilation compilation, System.Collections.Generic.IEnumerable<TypeSyntax> types)
        {
            foreach (TypeSyntax t in types)
            {
                TypeSyntax typeSyntax = t;
                if (typeSyntax is ArrayTypeSyntax arrayType)
                {
                    typeSyntax = arrayType.ElementType;
                }

                if (typeSyntax is PredefinedTypeSyntax predefined)
                {
                    SpecialType? specialType = _ToSpecialType(predefined.Keyword.Kind());
                    if (specialType == null)
                    {
                        return false;
                    }
                    var symbol = compilation.GetSpecialType(specialType.Value);
                    if (symbol == null || symbol.Kind == SymbolKind.ErrorType)
                    {
                        return false;
                    }
                    continue;
                }

                var typeName = typeSyntax.ToString();
                INamedTypeSymbol symbolByName = compilation.GetTypeByMetadataName(typeName);

                if (symbolByName == null || symbolByName.IsAbstract)
                    return false;
            }
            return true;
        }

        private static SpecialType? _ToSpecialType(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.BoolKeyword:
                    return SpecialType.System_Boolean;
                case SyntaxKind.SByteKeyword:
                    return SpecialType.System_SByte;
                case SyntaxKind.ByteKeyword:
                    return SpecialType.System_Byte;
                case SyntaxKind.ShortKeyword:
                    return SpecialType.System_Int16;
                case SyntaxKind.UShortKeyword:
                    return SpecialType.System_UInt16;
                case SyntaxKind.IntKeyword:
                    return SpecialType.System_Int32;
                case SyntaxKind.UIntKeyword:
                    return SpecialType.System_UInt32;
                case SyntaxKind.LongKeyword:
                    return SpecialType.System_Int64;
                case SyntaxKind.ULongKeyword:
                    return SpecialType.System_UInt64;
                case SyntaxKind.FloatKeyword:
                    return SpecialType.System_Single;
                case SyntaxKind.DoubleKeyword:
                    return SpecialType.System_Double;
                case SyntaxKind.DecimalKeyword:
                    return SpecialType.System_Decimal;
                case SyntaxKind.StringKeyword:
                    return SpecialType.System_String;
                case SyntaxKind.CharKeyword:
                    return SpecialType.System_Char;
                case SyntaxKind.ObjectKeyword:
                    return SpecialType.System_Object;
                default:
                    return null;
            }
        }


        static bool isSerializableType(int val)
        {
            var begin = (int)SpecialType.System_Boolean;
            var end = (int)SpecialType.System_String;
            return val >= begin && val <= end;
        }
        static bool isSerializableType(ITypeSymbol type)
        {


            return isSerializableType((int)type.SpecialType);
        }
    }
}
