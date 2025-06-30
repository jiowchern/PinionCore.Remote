using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PinionCore.Remote.Tools.Protocol.Sources.Extensions;
using static Microsoft.CodeAnalysis.SyntaxNodeExtensions;
namespace PinionCore.Remote.Tools.Protocol.Sources
{


    class SyntaxModifier
    {
        public static SyntaxModifier Create(Compilation com)
        {
            System.Collections.Generic.IEnumerable<INamedTypeSymbol> symbols = com.FindAllInterfaceSymbol();
            var memberIdProvider = new MemberIdProvider(symbols.Select(symbol => symbol.ToInferredInterface()));
            return Create(com, memberIdProvider);
        }
        public static SyntaxModifier Create(Compilation com, MemberIdProvider memberIdProvider)
        {
            return new SyntaxModifier(
                    new BlockModifiers.MethodVoid(com, memberIdProvider),
                    new BlockModifiers.MethodPinionCoreRemoteValue1(com, memberIdProvider),
                    new BlockModifiers.MethodPinionCoreRemoteValue0(com, memberIdProvider),
                    new BlockModifiers.EventSystemAction(com, memberIdProvider),
                    new BlockModifiers.PropertyPinionCoreRemoteBlock(com),
                    new Modifiers.EventFieldDeclarationSyntax(),
                    new Modifiers.PropertyFieldDeclarationSyntax()
                );
        }
        private readonly BlockModifiers.MethodVoid _MethodVoid;
        private readonly BlockModifiers.MethodPinionCoreRemoteValue1 _MethodPinionCoreRemoteValue1;
        private readonly BlockModifiers.MethodPinionCoreRemoteValue0 _MethodPinionCoreRemoteValue0;
        private readonly BlockModifiers.EventSystemAction _EventSystemAction;
        private readonly BlockModifiers.PropertyPinionCoreRemoteBlock _PropertyPinionCoreRemoteBlock;
        private readonly Modifiers.EventFieldDeclarationSyntax _EventFieldDeclarationSyntax;
        private readonly Modifiers.PropertyFieldDeclarationSyntax _PropertyFieldDeclarationSyntax;

        public SyntaxModifier(
            BlockModifiers.MethodVoid method_void,
            BlockModifiers.MethodPinionCoreRemoteValue1 method_regulus_remote_value1,
            BlockModifiers.MethodPinionCoreRemoteValue0 method_regulus_remote_value0,
            BlockModifiers.EventSystemAction event_system_action,
            BlockModifiers.PropertyPinionCoreRemoteBlock property_regulus_remote_block,
            Modifiers.EventFieldDeclarationSyntax event_field_declaration_syntax,
            Modifiers.PropertyFieldDeclarationSyntax property_field_declaration_syntax
            )
        {
            _MethodVoid = method_void;
            _MethodPinionCoreRemoteValue1 = method_regulus_remote_value1;
            _MethodPinionCoreRemoteValue0 = method_regulus_remote_value0;
            _EventSystemAction = event_system_action;
            _PropertyPinionCoreRemoteBlock = property_regulus_remote_block;
            _EventFieldDeclarationSyntax = event_field_declaration_syntax;
            _PropertyFieldDeclarationSyntax = property_field_declaration_syntax;
        }


        public ModResult Mod(ClassDeclarationSyntax type)
        {
            System.Collections.Generic.IEnumerable<MethodDeclarationSyntax> methods = type.DescendantNodes().OfType<MethodDeclarationSyntax>();

            type = _ModifyMethodParameters(type, methods);

            var propertys = new System.Collections.Generic.HashSet<PropertyDeclarationSyntax>(SyntaxNodeComparer.Default);
            var events = new System.Collections.Generic.HashSet<EventDeclarationSyntax>(SyntaxNodeComparer.Default);
            var typesOfSerialization = new System.Collections.Generic.HashSet<TypeSyntax>(SyntaxNodeComparer.Default);

            System.Collections.Generic.IEnumerable<BlockSyntax> blocks = type.DescendantNodes().OfType<BlockSyntax>();

            var replaceBlocks = new System.Collections.Generic.Dictionary<BlockSyntax, BlockSyntax>();
            var unprocessedBlocks = new System.Collections.Generic.List<Microsoft.CodeAnalysis.CSharp.Syntax.BlockSyntax>();
            foreach (BlockSyntax block in blocks)
            {
                System.Collections.Generic.IEnumerable<SyntaxNode> nodes = block.GetParentPathAndSelf();

                BlockModifiers.BlockAndEvent esa = _EventSystemAction.Mod(nodes);
                if (esa != null)
                {
                    events.Add(esa.Event);
                    replaceBlocks.Add(block, esa.Block);
                    continue;
                }

                BlockModifiers.BlockAndTypes methodVoid = _MethodVoid.Mod(nodes);
                if (methodVoid != null)
                {
                    typesOfSerialization.AddRange(methodVoid.Types);
                    replaceBlocks.Add(block, methodVoid.Block);
                    continue;
                }

                BlockModifiers.BlockAndTypes mrrv1 = _MethodPinionCoreRemoteValue1.Mod(nodes);
                if (mrrv1 != null)
                {
                    typesOfSerialization.AddRange(mrrv1.Types);
                    replaceBlocks.Add(block, mrrv1.Block);
                    continue;
                }
                BlockModifiers.BlockAndTypes mrrv0 = _MethodPinionCoreRemoteValue0.Mod(nodes);
                if (mrrv0 != null)
                {
                    typesOfSerialization.AddRange(mrrv0.Types);
                    replaceBlocks.Add(block, mrrv0.Block);
                    continue;
                }
                BlockModifiers.PropertyAndBlock prrb = _PropertyPinionCoreRemoteBlock.Mod(nodes);
                if (prrb != null)
                {
                    propertys.Add(prrb.Property);
                    replaceBlocks.Add(block, prrb.Block);
                    continue;
                }
                unprocessedBlocks.Add(block);
            }

            type = _ModifyBlocks(type, replaceBlocks);

            System.Collections.Generic.HashSet<EventDeclarationSyntax> eventDeclarationSyntaxes = events;

            foreach (EventDeclarationSyntax eds in eventDeclarationSyntaxes)
            {
                Modifiers.FieldAndTypes efds = _EventFieldDeclarationSyntax.Mod(eds);
                if (efds == null)
                {

                    continue;
                }


                type = type.AddMembers(efds.Field);
                typesOfSerialization.AddRange(efds.Types);
            }

            System.Collections.Generic.HashSet<PropertyDeclarationSyntax> propertyDeclarationSyntaxes = propertys;

            foreach (PropertyDeclarationSyntax pds in propertyDeclarationSyntaxes)
            {
                Modifiers.FieldAndTypes pfds = _PropertyFieldDeclarationSyntax.Mod(pds);
                if (pfds == null)
                {

                    continue;
                }

                type = type.AddMembers(pfds.Field);
                typesOfSerialization.AddRange(pfds.Types);
            }


            return new ModResult
            {
                Type = type,
                TypesOfSerialization = typesOfSerialization,
                UnprocessedBlocks = unprocessedBlocks.ToArray()
            };

        }

        private static ClassDeclarationSyntax _ModifyBlocks(ClassDeclarationSyntax type, System.Collections.Generic.Dictionary<BlockSyntax, BlockSyntax> replaceBlocks)
        {
            type = type.ReplaceNodes(replaceBlocks.Keys, (n1, n2) =>
            {
                if (replaceBlocks.ContainsKey(n1))
                {
                    return replaceBlocks[n1];
                }
                return n1;
            });
            return type;
        }

        private static ClassDeclarationSyntax _ModifyMethodParameters(ClassDeclarationSyntax type, System.Collections.Generic.IEnumerable<MethodDeclarationSyntax> methods)
        {
            var newParams = new System.Collections.Generic.Dictionary<ParameterSyntax, ParameterSyntax>();
            foreach (MethodDeclarationSyntax method in methods)
            {
                var idx = 0;
                foreach (ParameterSyntax p in method.ParameterList.Parameters)
                {
                    ParameterSyntax newP = p.WithIdentifier(SyntaxFactory.Identifier($"_{++idx}"));
                    newParams.Add(p, newP);
                }
            }

            type = type.ReplaceNodes(newParams.Keys, (n1, n2) =>
            {
                if (newParams.ContainsKey(n1))
                {
                    return newParams[n1];
                }
                return n1;
            });
            return type;
        }
    }


}
