using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
namespace PinionCore.Remote.Tools.Protocol.Sources.Modifiers
{
}
namespace PinionCore.Remote.Tools.Protocol.Sources.BlockModifiers
{

    internal class EventSystemAction
    {
        private readonly Compilation _Compilation;
        public readonly Dictionary<EventDeclarationSyntax,int > EventIds;
        public EventSystemAction(Compilation compilation)
        {
            this._Compilation = compilation;
            EventIds = new Dictionary<EventDeclarationSyntax,int >();
        }

        public BlockAndEvent Mod(System.Collections.Generic.IEnumerable<SyntaxNode> nodes)
        {

            var block = nodes.Skip(0).FirstOrDefault() as BlockSyntax;
            var ad = nodes.Skip(1).FirstOrDefault() as AccessorDeclarationSyntax;
            var al = nodes.Skip(2).FirstOrDefault() as AccessorListSyntax;
            var ed = nodes.Skip(3).FirstOrDefault() as EventDeclarationSyntax;
            var cd = nodes.Skip(4).FirstOrDefault() as ClassDeclarationSyntax;

            if (Extensions.SyntaxExtensions.AnyNull(block, ad, al, ed, cd))
            {
                return null;
            }
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
            if (sn is GenericNameSyntax gn)
            {
                if (!_Compilation.AllSerializable(gn.TypeArgumentList.Arguments))
                {
                    return null;
                }
            }


            NameSyntax ownerName = ed.ExplicitInterfaceSpecifier.Name;
            var name = $"_{ownerName}.{ed.Identifier}";
            name = name.Replace('.', '_');

            var ghostEventHandlerMethod = "";
            if (ad.IsKind(SyntaxKind.AddAccessorDeclaration))
            {
                ghostEventHandlerMethod = "Add";
            }
            else if (ad.IsKind(SyntaxKind.RemoveAccessorDeclaration))
            {
                ghostEventHandlerMethod = "Remove";
            }

            
            if(!EventIds.ContainsKey(ed))
            {
                
                EventIds.Add(ed, EventIds.Count + 1);
            }
            var eventId = EventIds[ed];


            BlockSyntax newBlock = SyntaxFactory.Block(SyntaxFactory.ParseStatement(
$@"
var handlerId = {name}.{ghostEventHandlerMethod}(value);
_{ghostEventHandlerMethod}EventEvent({eventId},handlerId);
"));

            return new BlockAndEvent
            {
                Block = newBlock,
                Event = ed
            };
        }





    }
}
