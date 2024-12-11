using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PinionCore.Remote.Tools.Protocol.Sources.Extensions;

namespace PinionCore.Remote.Tools.Protocol.Sources
{
    class ProjectSourceBuilder
    {
        public readonly IEnumerable<SyntaxTree> Sources;
        public readonly IEnumerable<ModResult> ClassAndTypess;

        public ProjectSourceBuilder(EssentialReference references)
        {
            Compilation compilation = references.Compilation;

            IEnumerable<INamedTypeSymbol> symbols = compilation.FindAllInterfaceSymbol(references.Tag);
            IEnumerable<InterfaceDeclarationSyntax> souls = symbols.Select(symbol => symbol.ToInferredInterface());


            var memberIdProvider = new MemberIdProvider(souls);
            var ghostBuilder = new GhostBuilder(SyntaxModifier.Create(compilation, memberIdProvider), symbols);
            ClassAndTypess = ghostBuilder.ClassAndTypess.ToArray();
            var name = ghostBuilder.Namespace;

            Microsoft.CodeAnalysis.CSharp.Syntax.NamespaceDeclarationSyntax root = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName($"NS{name}"));

            IEnumerable<SyntaxTree> eventProxys = ghostBuilder.EventProxys.Select(e => CSharpSyntaxTree.ParseText(root.AddMembers(e).NormalizeWhitespace().ToString(), null, $"{name}.{e.Identifier}.cs", System.Text.Encoding.UTF8));
            IEnumerable<SyntaxTree> ghosts = ghostBuilder.Ghosts.Select(e => CSharpSyntaxTree.ParseText(root.AddMembers(e).NormalizeWhitespace().ToString(), null, $"{name}.{e.Identifier}.cs", System.Text.Encoding.UTF8));

            var eventProviderCodeBuilder = new EventProviderCodeBuilder(eventProxys);


            var interfaceProviderCodeBuilder = new InterfaceProviderCodeBuilder(ghosts);
            var membermapCodeBuilder = new MemberMapCodeBuilder(souls, memberIdProvider);



            var protocolBuilder = new ProtocolBuilder(compilation, eventProviderCodeBuilder, interfaceProviderCodeBuilder, membermapCodeBuilder, new SerializableExtractor(compilation, ghostBuilder.Types));

            SyntaxTree[] protocolProviders = new ProtocoProviderlBuilder(compilation, protocolBuilder.ProtocolName).Trees;

            Sources = _UniteFilePath(name, ghosts.Concat(eventProxys).Concat(protocolProviders).Concat(new[] { protocolBuilder.Tree }));
        }



        private IEnumerable<SyntaxTree> _UniteFilePath(string name, IEnumerable<SyntaxTree> sources)
        {
            var index = 0;
            foreach (SyntaxTree source in sources)
            {
                yield return source.WithFilePath($"{name}.{index++}.cs");
            }
        }
    }
}
