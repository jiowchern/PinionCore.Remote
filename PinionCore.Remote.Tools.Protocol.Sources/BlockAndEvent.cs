using Microsoft.CodeAnalysis.CSharp.Syntax;
namespace PinionCore.Remote.Tools.Protocol.Sources.BlockModifiers
{
    class BlockAndEvent
    {
        public BlockSyntax Block;
        public EventDeclarationSyntax Event;
    }
}