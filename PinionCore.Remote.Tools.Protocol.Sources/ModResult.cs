using Microsoft.CodeAnalysis.CSharp.Syntax;
namespace PinionCore.Remote.Tools.Protocol.Sources
{
    public struct ModResult
    {
        public System.Collections.Generic.IEnumerable<TypeSyntax> TypesOfSerialization;
        public ClassDeclarationSyntax Type;
        public Microsoft.CodeAnalysis.CSharp.Syntax.BlockSyntax[] UnprocessedBlocks;

        public System.Collections.Generic.IEnumerable<T> GetSyntaxs<T>() where T : Microsoft.CodeAnalysis.SyntaxNode
        {
            foreach (BlockSyntax block in UnprocessedBlocks)
            {
                Microsoft.CodeAnalysis.SyntaxNode node = block;

                while (node != null && node.GetType() != typeof(T))
                {
                    node = node.Parent;
                }
                var ret = node as T;
                if (ret != null)
                    yield return ret;
            }
        }





    }


}
