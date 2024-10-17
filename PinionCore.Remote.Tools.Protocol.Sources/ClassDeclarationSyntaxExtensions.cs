using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static PinionCore.Remote.Tools.Protocol.Sources.Codes.PinionCoreRemoteIGhost;
namespace PinionCore.Remote.Tools.Protocol.Sources
{
    public static class ClassDeclarationSyntaxExtensions
    {        
        public static ClassDeclarationSyntax ImplementPinionCoreRemoteIGhost(this ClassDeclarationSyntax class_declaration)
        {
            var cd = class_declaration;            

         
            var type = SimpleBaseType(_PinionCoreRemoteIGhost);

            var baseList = cd.BaseList ?? SyntaxFactory.BaseList();

            var baseTypes = baseList.Types.ToArray();
            
            baseList = baseList.WithTypes(new SeparatedSyntaxList<BaseTypeSyntax>().Add(type).AddRange(baseTypes));
            return cd.WithBaseList(baseList).AddMembers(_CreateMembers(class_declaration.Identifier));
        }

        public static MemberDeclarationSyntax[] _CreateMembers(SyntaxToken name)
        {
            return new MemberDeclarationSyntax[]{
                    Field_HaveReturn,
                    Field_GhostId,
                    Constructor(name),
                    GetID,
                    IsReturnType,
                    GetInstance,
                    Field_CallMethodEvent,
                    EventCallMethodEvent,
                    Field_AddEventEvent,
                    EventAddEventEvent,
                    Field_RemoveEventEvent,
                    EventRemoveEventEvent
                };
        }

        
    }
}