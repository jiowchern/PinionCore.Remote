using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace PinionCore.Remote.Tools.Protocol.Sources.Codes
{
    public static partial class PinionCoreRemoteIGhost
    {
        public static readonly MemberDeclarationSyntax IsReturnType = MethodDeclaration(
                PredefinedType(
                    Token(SyntaxKind.BoolKeyword)
                ),
                Identifier("IsReturnType")
            )
            .WithExplicitInterfaceSpecifier(
                ExplicitInterfaceSpecifier(
                    _PinionCoreRemoteIGhost
                )
            )
            .WithBody(
                Block(
                    SingletonList(
                        (StatementSyntax)ReturnStatement(
                            IdentifierName("_HaveReturn")
                        )
                    )
                )
            );

    }
}
