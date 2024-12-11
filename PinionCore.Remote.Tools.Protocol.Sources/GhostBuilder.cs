using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using PinionCore.Remote.Tools.Protocol.Sources.Extensions;
namespace PinionCore.Remote.Tools.Protocol.Sources
{
    class GhostBuilder
    {

        public readonly IEnumerable<TypeSyntax> Types;
        public readonly IEnumerable<ClassDeclarationSyntax> Ghosts;
        public readonly IEnumerable<ClassDeclarationSyntax> EventProxys;

        public readonly string Namespace;
        public readonly IEnumerable<ModResult> ClassAndTypess;


        public GhostBuilder(SyntaxModifier modifier, IEnumerable<INamedTypeSymbol> symbols)
        {


            var builders = new Dictionary<INamedTypeSymbol, InterfaceInheritor>(SymbolEqualityComparer.Default);

            var souls = new List<InterfaceDeclarationSyntax>();
            foreach (KeyValuePair<INamedTypeSymbol, InterfaceInheritor> item in from i in symbols
                                                                                select new KeyValuePair<INamedTypeSymbol, InterfaceInheritor>(i, new InterfaceInheritor(i.ToInferredInterface())))
            {
                if (!builders.ContainsKey(item.Key))
                {
                    builders.Add(item.Key, item.Value);
                    souls.Add(item.Value.Base);
                }
            }


            var types = new System.Collections.Generic.List<TypeSyntax>();
            var ghosts = new System.Collections.Generic.List<ClassDeclarationSyntax>();
            var eventProxys = new System.Collections.Generic.List<ClassDeclarationSyntax>();

            var classAndTypess = new System.Collections.Generic.List<ModResult>();
            foreach (INamedTypeSymbol symbol in symbols)
            {

                var name = $"C{symbol.ToDisplayString().Replace('.', '_')}";
                ClassDeclarationSyntax type = SyntaxFactory.ClassDeclaration(name);
                type = type.WithOpenBraceToken(
                     SyntaxFactory.Token(
                        SyntaxFactory.TriviaList(
                            SyntaxFactory.Trivia(
                                SyntaxFactory.PragmaWarningDirectiveTrivia(
                                    SyntaxFactory.Token(SyntaxKind.DisableKeyword),
                                    true
                                )
                                .WithErrorCodes(
                                    SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                        SyntaxFactory.IdentifierName("CS0067")
                                    )
                                )
                            )
                        ),
                        SyntaxKind.OpenBraceToken,
                        SyntaxFactory.TriviaList()
                    )
                );
                foreach (INamedTypeSymbol i2 in symbol.AllInterfaces.Union(new[] { symbol }))
                {
                    InterfaceInheritor builder = builders[i2];
                    type = builder.Inherite(type);
                }

                eventProxys.AddRange(type.DescendantNodes().OfType<EventDeclarationSyntax>().Select(e => e.CreatePinionCoreRemoteIEventProxyCreater()));

                ModResult modResult = modifier.Mod(type);
                classAndTypess.Add(modResult);
                types.AddRange(modResult.TypesOfSerialization);
                type = modResult.Type;
                type = type.ImplementPinionCoreRemoteIGhost();
                type = type.WithCloseBraceToken(
                            SyntaxFactory.Token(
                                SyntaxFactory.TriviaList(
                                    SyntaxFactory.Trivia(
                                        SyntaxFactory.PragmaWarningDirectiveTrivia(
                                            SyntaxFactory.Token(SyntaxKind.RestoreKeyword),
                                            true
                                        )
                                        .WithErrorCodes(
                                            SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                                SyntaxFactory.IdentifierName("CS0067")
                                            )
                                        )
                                    )
                                ),
                                SyntaxKind.CloseBraceToken,
                                SyntaxFactory.TriviaList()
                            )
                        );
                ghosts.Add(type);

            }

            ClassAndTypess = classAndTypess;
            Types = new HashSet<TypeSyntax>(_WithOutNamespaceFilter(types), SyntaxNodeComparer.Default);
            Ghosts = new HashSet<ClassDeclarationSyntax>(ghosts, SyntaxNodeComparer.Default);
            EventProxys = new HashSet<ClassDeclarationSyntax>(eventProxys, SyntaxNodeComparer.Default);


            var all = string.Join("", Types.Select(t => t.ToFullString()).Union(Ghosts.Select(g => g.Identifier.ToFullString())).Union(EventProxys.Select(e => e.Identifier.ToFullString())));


            Namespace = $"PinionCoreRemoteProtocol{all.ToMd5().ToMd5String().Replace("-", "")}";


        }

        private IEnumerable<TypeSyntax> _WithOutNamespaceFilter(List<TypeSyntax> types)
        {

            foreach (TypeSyntax type in types)
            {

                yield return type;
            }

        }
    }

}
