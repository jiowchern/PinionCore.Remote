﻿using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
namespace PinionCore.Remote.Tools.Protocol.Sources.Modifiers
{
    internal class PropertyFieldDeclarationSyntax
    {


        public FieldAndTypes Mod(PropertyDeclarationSyntax pd)
        {

            NameSyntax ownerName = pd.ExplicitInterfaceSpecifier.Name;
            var name = $"_{ownerName}.{pd.Identifier}";
            name = name.Replace('.', '_');

            var types = new System.Collections.Generic.List<TypeSyntax>();
            var qn = pd.Type as QualifiedNameSyntax;

            if (qn == null)
                return null;

            if (qn.Left.ToString() != "PinionCore.Remote")
                return null;

            SimpleNameSyntax sn = qn.Right;
            if (sn == null)
                return null;

            if (sn.Identifier.ToString() != "Property" && sn.Identifier.ToString() != "Notifier")
                return null;

            if (!pd.DescendantNodes().OfType<AccessorDeclarationSyntax>().Any(a => a.Kind() == SyntaxKind.GetAccessorDeclaration))
                return null;

            if (qn.Right is GenericNameSyntax gn)
            {
                if (sn.Identifier.ToString() == "Property")
                    types.AddRange(gn.TypeArgumentList.Arguments);
            }


            return new FieldAndTypes() { Field = _CreateField(name, qn), Types = types };
        }

        private static FieldDeclarationSyntax _CreateField(string name, QualifiedNameSyntax qn)
        {
            return SyntaxFactory.FieldDeclaration(
                    SyntaxFactory.VariableDeclaration(
                        qn
                    )
                    .WithVariables(
                        SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(
                            SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier(name)
                            )
                            .WithInitializer(
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.ObjectCreationExpression(
                                        qn
                                    )
                                    .WithArgumentList(
                                       SyntaxFactory.ArgumentList()
                                    )
                                )
                            )
                        )
                    )
                );
        }
    }
}
