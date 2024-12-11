using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
namespace PinionCore.Remote.Tools.Protocol.Sources
{
    class MemberIdProvider
    {

        public readonly InterfaceDeclarationSyntax[] Souls;
        readonly Dictionary<string, int> _MemberIds;
        public readonly IReadOnlyDictionary<string, int> MemberIds;

        public MemberIdProvider(IEnumerable<InterfaceDeclarationSyntax> souls)
        {




            Souls = souls.ToArray();

            _MemberIds = new Dictionary<string, int>();
            MemberIds = _MemberIds;


            var nameProvider = new MembersNameProvider();

            foreach (EventFieldDeclarationSyntax member in from interfaceSyntax in souls
                                                           from methodSyntax in interfaceSyntax.DescendantNodes().OfType<EventFieldDeclarationSyntax>()
                                                           select methodSyntax)
            {
                _MemberIds.Add(nameProvider.GetEventName(member), _MemberIds.Count + 1);
            }

            foreach (MethodDeclarationSyntax member in from interfaceSyntax in souls
                                                       from methodSyntax in interfaceSyntax.DescendantNodes().OfType<MethodDeclarationSyntax>()
                                                       select methodSyntax)
            {
                _MemberIds.Add(nameProvider.GetMethodName(member), _MemberIds.Count + 1);
            }

            foreach (PropertyDeclarationSyntax member in from interfaceSyntax in souls
                                                         from methodSyntax in interfaceSyntax.DescendantNodes().OfType<PropertyDeclarationSyntax>()
                                                         select methodSyntax)
            {
                _MemberIds.Add(nameProvider.GetPropertyName(member), _MemberIds.Count + 1);
            }
        }

        public int GetId(EventDeclarationSyntax member)
        {
            var nameProvider = new MembersNameProvider();
            return _GetId(nameProvider.GetEventName(member));
        }
        public int GetId(EventFieldDeclarationSyntax member)
        {
            var nameProvider = new MembersNameProvider();
            return _GetId(nameProvider.GetEventName(member));
        }
        int _GetId(string member)
        {

            if (_MemberIds.TryGetValue(member, out var id))
            {
                return id;
            }


            throw new System.Exception($"Member not found {member}");
        }





    }

}
