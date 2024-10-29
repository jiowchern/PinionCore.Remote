using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace PinionCore.Remote.Tools.Protocol.Sources.Codes
{
    public static partial class PinionCoreRemoteIGhost
    {

        public static BlockSyntax _Block(SyntaxKind exp_1, string field_name)
        {
            return Block(
                        SingletonList<StatementSyntax>(
                                ExpressionStatement(
                                    AssignmentExpression(
                                        exp_1,
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            ThisExpression(),
                                            IdentifierName(field_name)
                                        ),
                                        IdentifierName("value")
                                    )
                                )
                            )
                        );
        }

        public static readonly QualifiedNameSyntax _PinionCoreRemoteEventNotifyCallback = QualifiedName(
                                QualifiedName(
                                    IdentifierName("PinionCore"),
                                    IdentifierName("Remote")
                                ),
                                IdentifierName("EventNotifyCallback")
                            );
        public static readonly QualifiedNameSyntax _PinionCoreRemoteCallMethodCallback = QualifiedName(
                            QualifiedName(
                                IdentifierName("PinionCore"),
                                IdentifierName("Remote")
                            ),
                            IdentifierName("CallMethodCallback")
                        );
        public static readonly QualifiedNameSyntax _PinionCoreRemoteIGhost = QualifiedName
                        (
                            QualifiedName(
                                IdentifierName("PinionCore"),
                                IdentifierName("Remote")
                            )
                            .WithDotToken(
                                Token(SyntaxKind.DotToken)
                            ),
                            IdentifierName("IGhost")
                        );


    }
}
