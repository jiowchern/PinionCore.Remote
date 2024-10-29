using Microsoft.CodeAnalysis;
namespace PinionCore.Remote.Tools.Protocol.Sources
{
    class IdentifyFinder
    {
        public readonly INamedTypeSymbol Tag;
        public IdentifyFinder(Compilation compilation)
        {
            Tag = compilation.GetTypeByMetadataName("PinionCore.Remote.Protocolable");
        }
    }
}
