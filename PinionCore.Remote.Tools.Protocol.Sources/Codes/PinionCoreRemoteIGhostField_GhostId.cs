using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace PinionCore.Remote.Tools.Protocol.Sources.Codes
{
    public static partial class PinionCoreRemoteIGhost
    {
        public static readonly MemberDeclarationSyntax Field_GhostId = FieldDeclaration(
                       VariableDeclaration(
                           PredefinedType(
                               Token(SyntaxKind.LongKeyword)
                           )
                       )
                       .WithVariables(
                           SingletonSeparatedList<VariableDeclaratorSyntax>(
                               VariableDeclarator(
                                   Identifier("_GhostId")
                               )
                           )
                       )
                   )
                   .WithModifiers(
                       TokenList(
                           Token(SyntaxKind.ReadOnlyKeyword)
                       )
                   );
    }
}
