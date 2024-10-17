using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace PinionCore.Remote.Tools.Protocol.Sources.BlockModifiers
{
    class BlockAndTypes
    {
        public BlockSyntax Block;
        public System.Collections.Generic.IEnumerable<TypeSyntax> Types;
    }
}