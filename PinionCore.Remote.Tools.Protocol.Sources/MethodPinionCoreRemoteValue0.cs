using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PinionCore.Remote.Tools.Protocol.Sources.BlockModifiers
{
    internal class MethodPinionCoreRemoteValue0
    {
        private readonly Compilation _Compilation;
        private readonly MemberIdProvider _MemberIdProvider;

        public MethodPinionCoreRemoteValue0(Compilation compilation, MemberIdProvider memberIdProvider)
        {
            this._Compilation = compilation;
            _MemberIdProvider = memberIdProvider;
        }
        public BlockAndTypes Mod(System.Collections.Generic.IEnumerable<SyntaxNode> nodes)
        {
            var block = nodes.Skip(0).FirstOrDefault() as BlockSyntax;
            var md = nodes.Skip(1).FirstOrDefault() as MethodDeclarationSyntax;
            var cd = nodes.Skip(2).FirstOrDefault() as ClassDeclarationSyntax;


            if (Extensions.SyntaxExtensions.AnyNull(block, md, cd))
            {
                return null;
            }
            if (!_Compilation.AllSerializable(md.ParameterList.Parameters.Select(p => p.Type)))
                return null;
            if ((from p in md.ParameterList.Parameters
                 from m in p.Modifiers
                 where m.IsKind(SyntaxKind.OutKeyword)
                 select m).Any())
                return null;

            var interfaceCode = md.ExplicitInterfaceSpecifier.Name.ToFullString();
            var methodCode = md.Identifier.ToFullString();
            var methodCallParamsCode = string.Join(",", from p in md.ParameterList.Parameters select p.Identifier.ToFullString());
            TypeSyntax returnType = md.ReturnType;
            var qn = returnType as QualifiedNameSyntax;

            if (qn == null)
                return null;

            if (qn.Left.ToString() != "PinionCore.Remote")
                return null;

            var gn = qn.Right as SimpleNameSyntax;
            if (gn == null)
                return null;

            if (gn.Identifier.ToString() != "Value")
                return null;
            var id = _MemberIdProvider.DisrbutionIdWithGhost(md);

            return new BlockAndTypes
            {
                Block = SyntaxFactory.Block(SyntaxFactory.ParseStatement(
                                $@"
var returnValue = new PinionCore.Remote.Value(true);

this._CallMethodEvent({id}, new object[] {{{methodCallParamsCode}}} , returnValue);                    
return returnValue;
                                            ")),
                Types = new TypeSyntax[0]
            };

        }
    }
}
