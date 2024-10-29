using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PinionCore.Remote.Tools.Protocol.Sources
{
    internal class ProtocoProviderlBuilder
    {

        public readonly SyntaxTree[] Trees;

        public ProtocoProviderlBuilder(Compilation compilation, string protocol_name)
        {

            var trees = new System.Collections.Generic.List<SyntaxTree>();
            foreach (SyntaxTree tree in compilation.SyntaxTrees)
            {
                SyntaxNode root = tree.GetRoot();
                SemanticModel model = compilation.GetSemanticModel(tree);
                System.Collections.Generic.IEnumerable<MethodDeclarationSyntax> methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
                foreach (MethodDeclarationSyntax method in methods)
                {
                    if (!method.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword)))
                        continue;

                    foreach (AttributeSyntax attribute in method.AttributeLists.SelectMany(a => a.Attributes))
                    {
                        SymbolInfo info = model.GetSymbolInfo(attribute);
                        if (info.Symbol == null)
                            continue;

                        var name = info.Symbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        if (name != "global::PinionCore.Remote.Protocol.CreaterAttribute")
                        {
                            continue;
                        }

                        ParameterSyntax firstParam = method.ParameterList.Parameters.FirstOrDefault();
                        if (firstParam == null)
                            continue;

                        StatementSyntax stat = SyntaxFactory.ParseStatement($"{firstParam.Identifier} = new {protocol_name}();");
                        BlockSyntax block = SyntaxFactory.Block(stat);

                        MethodDeclarationSyntax newMethod = SyntaxFactory.MethodDeclaration(method.ReturnType, method.Identifier);
                        newMethod = newMethod.WithParameterList(method.ParameterList);
                        newMethod = newMethod.WithModifiers(method.Modifiers);
                        newMethod = newMethod.WithBody(block);

                        var parent = method.Parent as ClassDeclarationSyntax;
                        ClassDeclarationSyntax newClass = SyntaxFactory.ClassDeclaration(parent.Identifier);
                        newClass = newClass.WithModifiers(parent.Modifiers);
                        newClass = newClass.WithMembers(new SyntaxList<MemberDeclarationSyntax>(new[] { newMethod }));



                        NamespaceDeclarationSyntax @namespace = parent.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();


                        CompilationUnitSyntax compilationUnit = parent.Ancestors().OfType<CompilationUnitSyntax>().FirstOrDefault();
                        CompilationUnitSyntax syntaxFactory = SyntaxFactory.CompilationUnit();
                        syntaxFactory = syntaxFactory.WithUsings(compilationUnit.Usings);
                        if (@namespace != null)
                        {
                            NamespaceDeclarationSyntax newNamespace = SyntaxFactory.NamespaceDeclaration(@namespace.Name);
                            newNamespace = newNamespace.AddMembers(newClass);


                            syntaxFactory = syntaxFactory.AddMembers(newNamespace);
                        }
                        else
                        {
                            syntaxFactory = syntaxFactory.AddMembers(newClass);
                        }

                        SyntaxTree t = SyntaxFactory.ParseSyntaxTree(syntaxFactory.NormalizeWhitespace().ToFullString(), null, $"ProtocoProviderlBuilder.{protocol_name}.cs", Encoding.UTF8);
                        trees.Add(t);
                    }
                }
            }
            Trees = trees.ToArray();
        }
    }
}
