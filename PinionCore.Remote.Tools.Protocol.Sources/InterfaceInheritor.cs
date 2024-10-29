using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PinionCore.Remote.Tools.Protocol.Sources.Extensions;

namespace PinionCore.Remote.Tools.Protocol.Sources
{
    public class InterfaceInheritor
    {
        readonly string _NamePath;
        public readonly InterfaceDeclarationSyntax Base;

        public readonly BlockSyntax Expression;


        public InterfaceInheritor(InterfaceDeclarationSyntax @base) : this(@base, SyntaxFactory.Block(SyntaxFactory.ExpressionStatement(SyntaxFactory.ThrowExpression(SyntaxFactory.ParseExpression("new PinionCore.Remote.Exceptions.NotSupportedException()")))))
        {

        }
        public InterfaceInheritor(InterfaceDeclarationSyntax @base, BlockSyntax expression)
        {
            Expression = expression;
            _NamePath = @base.GetNamePath();
            this.Base = @base;
        }

        public ClassDeclarationSyntax Inherite(ClassDeclarationSyntax class_syntax)
        {

            InterfaceDeclarationSyntax interfaceDeclaration = Base;
            var namePath = _NamePath;


            ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier = SyntaxFactory.ExplicitInterfaceSpecifier(SyntaxFactory.ParseName(namePath));

            ClassDeclarationSyntax classSyntax = class_syntax;
            BaseListSyntax bases = classSyntax.BaseList ?? SyntaxFactory.BaseList();
            bases = bases.AddTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(namePath)));
            classSyntax = classSyntax.WithBaseList(bases);

            System.Collections.Generic.IEnumerable<MemberDeclarationSyntax> nodes = interfaceDeclaration.DescendantNodes().OfType<MemberDeclarationSyntax>();

            classSyntax = _InheriteMethods(explicitInterfaceSpecifier, classSyntax, nodes);

            classSyntax = _InheriteProperties(explicitInterfaceSpecifier, classSyntax, nodes);

            classSyntax = _InheriteEvents(explicitInterfaceSpecifier, classSyntax, nodes);

            classSyntax = _InheriteIndexs(explicitInterfaceSpecifier, classSyntax, nodes);

            return classSyntax;
        }

        private ClassDeclarationSyntax _InheriteIndexs(ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier, ClassDeclarationSyntax classSyntax, System.Collections.Generic.IEnumerable<MemberDeclarationSyntax> nodes)
        {
            foreach (IndexerDeclarationSyntax member in nodes.OfType<IndexerDeclarationSyntax>())
            {
                IndexerDeclarationSyntax node = member;
                node = node.WithExplicitInterfaceSpecifier(explicitInterfaceSpecifier);

                System.Collections.Generic.IEnumerable<AccessorDeclarationSyntax> accessors = from a in node.AccessorList.Accessors
                                                                                              select a.WithBody(Expression);
                node = node.WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(accessors)));

                classSyntax = classSyntax.AddMembers(node);
            }

            return classSyntax;
        }

        private ClassDeclarationSyntax _InheriteEvents(ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier, ClassDeclarationSyntax classSyntax, System.Collections.Generic.IEnumerable<MemberDeclarationSyntax> nodes)
        {
            foreach (EventFieldDeclarationSyntax member in nodes.OfType<EventFieldDeclarationSyntax>())
            {
                EventFieldDeclarationSyntax node = member;
                EventDeclarationSyntax eventDeclaration = SyntaxFactory.EventDeclaration(node.Declaration.Type, node.Declaration.Variables[0].Identifier);

                eventDeclaration = eventDeclaration.WithExplicitInterfaceSpecifier(explicitInterfaceSpecifier);
                AccessorListSyntax accessors = SyntaxFactory.AccessorList();
                accessors = accessors.AddAccessors(SyntaxFactory.AccessorDeclaration(SyntaxKind.AddAccessorDeclaration).WithBody(Expression));
                accessors = accessors.AddAccessors(SyntaxFactory.AccessorDeclaration(SyntaxKind.RemoveAccessorDeclaration).WithBody(Expression));
                eventDeclaration = eventDeclaration.WithAccessorList(accessors);
                classSyntax = classSyntax.AddMembers(eventDeclaration);
            }

            return classSyntax;
        }

        private ClassDeclarationSyntax _InheriteProperties(ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier, ClassDeclarationSyntax classSyntax, System.Collections.Generic.IEnumerable<MemberDeclarationSyntax> nodes)
        {
            foreach (PropertyDeclarationSyntax member in nodes.OfType<PropertyDeclarationSyntax>())
            {
                PropertyDeclarationSyntax node = member;
                node = node.WithExplicitInterfaceSpecifier(explicitInterfaceSpecifier);

                System.Collections.Generic.IEnumerable<AccessorDeclarationSyntax> accessors = from a in node.AccessorList.Accessors
                                                                                              select a.WithBody(Expression);

                node = node.WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(accessors)));

                classSyntax = classSyntax.AddMembers(node);

            }

            return classSyntax;
        }

        private ClassDeclarationSyntax _InheriteMethods(ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier, ClassDeclarationSyntax classSyntax, System.Collections.Generic.IEnumerable<MemberDeclarationSyntax> nodes)
        {
            foreach (MethodDeclarationSyntax member in nodes.OfType<MethodDeclarationSyntax>())
            {
                MethodDeclarationSyntax node = member;

                node = node.WithExplicitInterfaceSpecifier(explicitInterfaceSpecifier);
                node = node.WithBody(Expression);
                classSyntax = classSyntax.AddMembers(node);
            }

            return classSyntax;
        }
    }


}
