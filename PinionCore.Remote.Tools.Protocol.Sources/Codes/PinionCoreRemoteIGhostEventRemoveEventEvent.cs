using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace PinionCore.Remote.Tools.Protocol.Sources.Codes
{
    public static partial class PinionCoreRemoteIGhost
    {
        public static readonly MemberDeclarationSyntax EventRemoveEventEvent = EventDeclaration(
                        _PinionCoreRemoteEventNotifyCallback,
                        Identifier("RemoveEventEvent")
                    )
                    .WithExplicitInterfaceSpecifier(
                        ExplicitInterfaceSpecifier(
                            _PinionCoreRemoteIGhost
                        )
                    )
                    .WithAccessorList(
                        AccessorList(
                            List(
                                new AccessorDeclarationSyntax[]{
                                    AccessorDeclaration(
                                        SyntaxKind.AddAccessorDeclaration
                                    )
                                    .WithBody(
                                        _Block(SyntaxKind.AddAssignmentExpression, "_RemoveEventEvent")
                                    ),
                                    AccessorDeclaration(
                                        SyntaxKind.RemoveAccessorDeclaration
                                    )
                                    .WithBody(
                                        _Block(SyntaxKind.SubtractAssignmentExpression, "_RemoveEventEvent")
                                    )
                                }
                            )
                        )
                    );
    }
}
