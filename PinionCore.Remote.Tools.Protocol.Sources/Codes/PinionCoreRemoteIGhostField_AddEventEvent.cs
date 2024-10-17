using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace PinionCore.Remote.Tools.Protocol.Sources.Codes
{
    public static partial class PinionCoreRemoteIGhost
    {
        public static readonly MemberDeclarationSyntax Field_AddEventEvent = EventFieldDeclaration(
                        VariableDeclaration(
                            _PinionCoreRemoteEventNotifyCallback
                        )
                        .WithVariables(
                            SingletonSeparatedList(
                                VariableDeclarator(
                                    Identifier("_AddEventEvent")
                                )
                            )
                        )
                    )
                    .WithModifiers(
                        TokenList(
                            Token(SyntaxKind.PrivateKeyword)
                        )
                    );
    }
}