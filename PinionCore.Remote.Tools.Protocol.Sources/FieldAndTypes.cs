using Microsoft.CodeAnalysis.CSharp.Syntax;
namespace PinionCore.Remote.Tools.Protocol.Sources.Modifiers
{
    class FieldAndTypes
    {
        public FieldDeclarationSyntax Field;
        public System.Collections.Generic.IEnumerable<TypeSyntax> Types;
    }
}
