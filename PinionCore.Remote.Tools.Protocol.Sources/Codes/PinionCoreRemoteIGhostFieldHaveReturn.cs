using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace PinionCore.Remote.Tools.Protocol.Sources.Codes
{
    public static partial class PinionCoreRemoteIGhost
    {
        public static readonly MemberDeclarationSyntax Field_HaveReturn = FieldDeclaration(
                       VariableDeclaration(
                           PredefinedType(
                               Token(SyntaxKind.BoolKeyword)
                           )
                       )
                       .WithVariables(
                           SingletonSeparatedList<VariableDeclaratorSyntax>(
                               VariableDeclarator(
                                   Identifier("_HaveReturn")
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
