
namespace PinionCore.Remote.Tools.Protocol.Sources
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    
    public class MemberIdProvider
    {
        private readonly Dictionary<string, int> _memberIds;
        private int _currentId;
       

        public MemberIdProvider(IEnumerable<InterfaceDeclarationSyntax> souls)
        {
            _memberIds = new Dictionary<string, int>();
       
            _currentId = 1;

            foreach (var soul in souls)
            {
                var namespaceName = GetNamespaceName(soul);
       
                InitializeMemberIds(soul);
            }
        }

        private void InitializeMemberIds(InterfaceDeclarationSyntax soul)
        {
            var namespaceName = GetNamespaceName(soul);
            
            // Process methods
            foreach (var method in soul.Members.OfType<MethodDeclarationSyntax>())
            {
                var key = GenerateKey(namespaceName, soul.Identifier.Text, method.Identifier.Text, "method");
                _memberIds[key] = _currentId++;
            }

            // Process events
            foreach (var evt in soul.Members.OfType<EventFieldDeclarationSyntax>())
            {
                var evtName = evt.Declaration.Variables.First().Identifier.Text;
                var key = GenerateKey(namespaceName, soul.Identifier.Text, evtName , "event");
                _memberIds[key] = _currentId++;
            }

            // Process properties
            foreach (var property in soul.Members.OfType<PropertyDeclarationSyntax>())
            {
                var key = GenerateKey(namespaceName, soul.Identifier.Text, property.Identifier.Text, "property");
                _memberIds[key] = _currentId++;
            }

            // Process indexers
            foreach (var indexer in soul.Members.OfType<IndexerDeclarationSyntax>())
            {
                var key = GenerateKey(namespaceName, soul.Identifier.Text, "this", "index");
                _memberIds[key] = _currentId++;
            }
        }

        private string GetNamespaceName(SyntaxNode node)
        {
            var namespaceNode = node.AncestorsAndSelf().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
            return namespaceNode != null ? $"{namespaceNode.Name.ToString()}." : "";
        }

        private string GenerateKey(string namespaceName, string typeName, string memberName, string memberType)
        {
            return $"{namespaceName}{typeName}:{memberType}:{memberName}";
        }
        public int GetIdWithSoul(EventFieldDeclarationSyntax mem)
        {
            var memberName = mem.Declaration.Variables.First().Identifier.Text;
            return _GetIdWithSoul(memberName, "event", mem);
        }
        public int GetIdWithSoul(MethodDeclarationSyntax mem)
        {
            var memberName = mem.Identifier.Text;

            return _GetIdWithSoul(memberName,"method", mem);
        }
        private int _GetIdWithSoul(string member_name , string member_type, MemberDeclarationSyntax mem)
        {
            var type = mem.Parent as InterfaceDeclarationSyntax;
            if (type == null)
            {
                throw new InvalidOperationException("Member does not belong to a interface.");
            }
            var namespaceName = GetNamespaceName(mem);

            var typeName = type.Identifier.Text;


            var key = $"{namespaceName}{typeName}:{member_type}:{member_name}";

            if (_memberIds.TryGetValue(key, out var id))
            {
                return id;
            }

            throw new KeyNotFoundException($"Member ID not found for key: {key}");
            
        }
        public int DisrbutionIdWithGhost(MemberDeclarationSyntax member)
        {
            if (member is MethodDeclarationSyntax method)
            {
                return _GetIdWithGhost(method , method.ExplicitInterfaceSpecifier , "method");
            }
            else if (member is EventDeclarationSyntax evt)
            {
                return _GetIdWithGhost(evt, evt.ExplicitInterfaceSpecifier, "event");
            }
            else if (member is PropertyDeclarationSyntax property)
            {
                return _GetIdWithGhost(property, property.ExplicitInterfaceSpecifier, "property");
            }
            else if (member is IndexerDeclarationSyntax indexer)
            {
                return _GetIdWithGhost(indexer, indexer.ExplicitInterfaceSpecifier, "index");
            }

            throw new ArgumentException("Unsupported member type.", nameof(member));
        }

        private int _GetIdWithGhost(MemberDeclarationSyntax member,ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifierSyntax , string memberType)
        {

            if (explicitInterfaceSpecifierSyntax == null)
            {
                throw new InvalidOperationException("Member does not belong to a interface.");
            }
            var containingType = member.Parent as ClassDeclarationSyntax;
            if (containingType == null)
            {
                throw new InvalidOperationException("Member does not belong to a class or interface.");
            }
            var namespaceAndTypeName = explicitInterfaceSpecifierSyntax.Name.ToFullString();
            string memberName = _GetMemberName(member);

            var key = $"{namespaceAndTypeName}:{memberType}:{memberName}";
            
            if (_memberIds.TryGetValue(key, out var id))
            {
                return id;
            }

            throw new KeyNotFoundException($"Member ID not found for key: {key}");
        }

        private static string _GetMemberName(MemberDeclarationSyntax member)
        {
            string memberName;
            if (member is MethodDeclarationSyntax methodMember)
            {
                memberName = methodMember.Identifier.Text;
            }
            else if (member is EventDeclarationSyntax eventMember)
            {
                memberName = eventMember.Identifier.Text;
            }
            else if (member is PropertyDeclarationSyntax propertyMember)
            {
                memberName = propertyMember.Identifier.Text;
            }
            else if (member is IndexerDeclarationSyntax)
            {
                memberName = "this";
            }
            else
            {
                throw new ArgumentException("Unsupported member type.", nameof(member));
            }

            return memberName;
        }
    }


}

