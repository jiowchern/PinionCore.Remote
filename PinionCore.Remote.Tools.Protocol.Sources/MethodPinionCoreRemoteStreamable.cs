using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PinionCore.Remote.Tools.Protocol.Sources.BlockModifiers
{
    internal class MethodPinionCoreRemoteStreamable
    {
        private readonly Compilation _Compilation;
        private readonly MemberIdProvider _MemberIdProvider;

        public MethodPinionCoreRemoteStreamable(Compilation compilation, MemberIdProvider memberIdProvider)
        {
            _Compilation = compilation;
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

            if (md.ParameterList.Parameters.Count != 3)
            {
                return null;
            }

            if (!_Compilation.AllSerializable(md.ParameterList.Parameters.Select(p => p.Type)))
            {
                return null;
            }

            if ((from p in md.ParameterList.Parameters
                 from m in p.Modifiers
                 where m.IsKind(SyntaxKind.OutKeyword)
                 select m).Any())
            {
                return null;
            }

            ParameterSyntax p0 = md.ParameterList.Parameters[0];
            ParameterSyntax p1 = md.ParameterList.Parameters[1];
            ParameterSyntax p2 = md.ParameterList.Parameters[2];

            if (!_IsByteArray(p0.Type))
            {
                return null;
            }

            if (!_IsIntType(p1.Type))
            {
                return null;
            }

            if (!_IsIntType(p2.Type))
            {
                return null;
            }

            if (!_IsAwaitableInt(md.ReturnType))
            {
                return null;
            }

            var id = _MemberIdProvider.DisrbutionIdWithGhost(md);
            var pNames = md.ParameterList.Parameters.Select(p => p.Identifier.ToFullString()).ToArray();

            return new BlockAndTypes
            {
                Block = SyntaxFactory.Block(SyntaxFactory.ParseStatement($@"
var returnValue = new PinionCore.Remote.Value<int>();

this._CallStreamMethodEvent({id}, {pNames[0]}, {pNames[1]}, {pNames[2]}, returnValue);
return returnValue;
")),
                Types = md.ParameterList.Parameters.Select(p => p.Type)
            };
        }

        private static bool _IsByteArray(TypeSyntax type)
        {
            if (type is ArrayTypeSyntax arrayType &&
                arrayType.RankSpecifiers.Count == 1)
            {
                return _IsNamedType(arrayType.ElementType, "byte", "System.Byte");
            }
            return false;
        }

        private static bool _IsIntType(TypeSyntax type)
        {
            return _IsNamedType(type, "int", "System.Int32");
        }

        private static bool _IsAwaitableInt(TypeSyntax type)
        {
            if (!_TryGetSimpleName(type, out SimpleNameSyntax simpleName))
            {
                return false;
            }

            if (simpleName is GenericNameSyntax generic &&
                generic.Identifier.Text == "IAwaitableSource" &&
                generic.TypeArgumentList.Arguments.Count == 1)
            {
                var argument = generic.TypeArgumentList.Arguments[0];
                if (_IsNamedType(argument, "int", "System.Int32"))
                {
                    if (_TryGetNamespace(type, out string ns))
                    {
                        return ns == "PinionCore.Remote";
                    }
                }
            }
            return false;
        }

        private static bool _IsNamedType(TypeSyntax type, params string[] candidates)
        {
            if (_TryGetSimpleName(type, out SimpleNameSyntax nameSyntax))
            {
                foreach (string candidate in candidates)
                {
                    if (string.Equals(nameSyntax.Identifier.Text, candidate, System.StringComparison.Ordinal))
                    {
                        return true;
                    }
                    if (candidate.Contains('.'))
                    {
                        string simple = candidate.Split('.').Last();
                        if (string.Equals(nameSyntax.Identifier.Text, simple, System.StringComparison.Ordinal))
                        {
                            if (_TryGetNamespace(type, out string ns))
                            {
                                if (candidate.StartsWith(ns))
                                    return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        private static bool _TryGetSimpleName(TypeSyntax type, out SimpleNameSyntax name)
        {
            name = null;
            TypeSyntax current = type;
            while (current != null)
            {
                switch (current)
                {
                    case SimpleNameSyntax simple:
                        name = simple;
                        return true;
                    case QualifiedNameSyntax qualified:
                        current = qualified.Right;
                        break;
                    case AliasQualifiedNameSyntax aliasQualified:
                        current = aliasQualified.Name;
                        break;
                    case ArrayTypeSyntax arrayType:
                        current = arrayType.ElementType;
                        break;
                    default:
                        return false;
                }
            }
            return false;
        }

        private static bool _TryGetNamespace(TypeSyntax type, out string namespaceName)
        {
            namespaceName = null;
            var parts = new System.Collections.Generic.List<string>();
            TypeSyntax current = type;
            while (current != null)
            {
                switch (current)
                {
                    case QualifiedNameSyntax qualified:
                        parts.Add(qualified.Right.Identifier.Text);
                        current = qualified.Left;
                        break;
                    case AliasQualifiedNameSyntax aliasQualified:
                        parts.Add(aliasQualified.Name.Identifier.Text);
                        current = aliasQualified.Alias;
                        break;
                    case IdentifierNameSyntax identifier:
                        parts.Add(identifier.Identifier.Text);
                        current = null;
                        break;
                    case GenericNameSyntax generic:
                        parts.Add(generic.Identifier.Text);
                        current = null;
                        break;
                    case ArrayTypeSyntax arrayType:
                        current = arrayType.ElementType;
                        break;
                    default:
                        current = null;
                        break;
                }
            }
            if (parts.Count == 0)
            {
                return false;
            }
            parts.Reverse();
            if (parts.Count > 1)
            {
                namespaceName = string.Join(".", parts.Take(parts.Count - 1));
                namespaceName = namespaceName.Replace("global.", string.Empty);
                return true;
            }
            namespaceName = string.Empty;
            return true;
        }
    }
}
