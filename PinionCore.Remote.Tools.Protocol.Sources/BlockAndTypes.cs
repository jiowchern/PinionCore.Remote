using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PinionCore.Remote.Tools.Protocol.Sources.BlockModifiers
{
    class BlockAndTypes
    {
        public BlockSyntax Block;
        public System.Collections.Generic.IEnumerable<TypeSyntax> Types;
    }
}
