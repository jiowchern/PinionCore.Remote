﻿using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
namespace PinionCore.Remote.Tools.Protocol.Sources.Modifiers
{
    internal class EventFieldDeclarationSyntax
    {

        public FieldAndTypes Mod(EventDeclarationSyntax ed)
        {

            NameSyntax ownerName = ed.ExplicitInterfaceSpecifier.Name;
            var name = $"_{ownerName}.{ed.Identifier}";
            name = name.Replace('.', '_');

            var types = new System.Collections.Generic.List<TypeSyntax>();
            var qn = ed.Type as QualifiedNameSyntax;
            if (qn == null)
                return null;

            if (qn.Left.ToString() != "System")
                return null;

            SimpleNameSyntax sn = qn.Right;
            if (sn == null)
                return null;

            if (sn.Identifier.ToString() != "Action")
                return null;


            if (qn.Right is GenericNameSyntax gn)
            {
                types.AddRange(gn.TypeArgumentList.Arguments);
            }


            return new FieldAndTypes() { Field = _CreateGhostEventHandler(name), Types = types };
        }

        private static FieldDeclarationSyntax _CreateGhostEventHandler(string name)
        {
            return SyntaxFactory.FieldDeclaration(
             SyntaxFactory.VariableDeclaration(
                 SyntaxFactory.QualifiedName(
                     SyntaxFactory.QualifiedName(
                         SyntaxFactory.IdentifierName("PinionCore"),
                         SyntaxFactory.IdentifierName("Remote")),
                     SyntaxFactory.IdentifierName("GhostEventHandler")))
             .WithVariables(
                 SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(
                     SyntaxFactory.VariableDeclarator(
                         SyntaxFactory.Identifier(name))
                     .WithInitializer(
                         SyntaxFactory.EqualsValueClause(
                             SyntaxFactory.ObjectCreationExpression(
                                 SyntaxFactory.QualifiedName(
                                     SyntaxFactory.QualifiedName(
                                         SyntaxFactory.IdentifierName("PinionCore"),
                                         SyntaxFactory.IdentifierName("Remote")),
                                     SyntaxFactory.IdentifierName("GhostEventHandler")))
                             .WithArgumentList(
                                 SyntaxFactory.ArgumentList()))))));
        }
    }
}
