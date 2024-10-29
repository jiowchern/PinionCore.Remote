using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace PinionCore.Remote.Tools.Protocol.Sources.Codes
{
    public static partial class PinionCoreRemoteIGhost
    {
        public static readonly MemberDeclarationSyntax GetInstance = MethodDeclaration(
                        PredefinedType(
                            Token(SyntaxKind.ObjectKeyword)
                        ),
                        Identifier("GetInstance")
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
                                    ThisExpression()
                                )
                            )
                        )
                    );
    }
}
