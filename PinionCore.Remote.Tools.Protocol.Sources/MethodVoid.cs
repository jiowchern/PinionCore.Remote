﻿using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PinionCore.Remote.Tools.Protocol.Sources.BlockModifiers
{


    internal class MethodVoid
    {
        private readonly Compilation _Compilation;

        public MethodVoid(Compilation compilation)
        {
            this._Compilation = compilation;
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

            NameSyntax interfaceCode = md.ExplicitInterfaceSpecifier.Name;
            var methodCode = md.Identifier.ToFullString();
            var methodCallParamsCode = string.Join(",", from p in md.ParameterList.Parameters select p.Identifier.ToFullString());
            TypeSyntax returnType = md.ReturnType;
            var pt = returnType as PredefinedTypeSyntax;

            if (pt == null)
                return null;

            if (!pt.Keyword.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.VoidKeyword))
            {
                return null;
            }

            return new BlockAndTypes
            {
                Block = SyntaxFactory.Block(SyntaxFactory.ParseStatement(
$@"var info = typeof({interfaceCode}).GetMethod(""{methodCode}"");
this._CallMethodEvent(info , new object[] {{{methodCallParamsCode}}} , null);", 0)),
                Types = from p in md.ParameterList.Parameters select p.Type
            };



        }

    }
}
