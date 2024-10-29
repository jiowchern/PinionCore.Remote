using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace PinionCore.Remote.Tools.Protocol.Sources
{
    public class SyntaxTreeBuilder
    {
        public readonly SyntaxTree Tree;
        public SyntaxTreeBuilder(SourceText source)
        {
            Tree = SyntaxFactory.ParseSyntaxTree(source);


        }

        public System.Collections.Generic.IEnumerable<InterfaceDeclarationSyntax> GetInterfaces(string name)
        {



            System.Collections.Generic.IEnumerable<InterfaceDeclarationSyntax> ids = from i in Tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>()
                                                                                     where i.Identifier.ToString() == name
                                                                                     select i;
            ;

            return ids;
        }
    }
}
