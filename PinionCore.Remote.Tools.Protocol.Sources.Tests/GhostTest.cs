using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;

namespace PinionCore.Remote.Tools.Protocol.Sources.Tests
{
    class GhostTest
    {
        private readonly SyntaxTree[] _Souls;

        private readonly IEnumerable<SyntaxTree> _Sources;

        public GhostTest(params SyntaxTree[] souls)
        {

            _Souls = souls;
            var assemblyName = "TestProject";

            IEnumerable<MetadataReference> references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(PinionCore.Remote.Property<>).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(PinionCore.Remote.Value<>).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Action).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Action<,,,,,,,,>).GetTypeInfo().Assembly.Location),
            };
            var compilation = CSharpCompilation.Create(assemblyName, souls, references);

            _Sources = new ProjectSourceBuilder(new EssentialReference(compilation, null)).Sources;
        }
        


        public async Task RunAsync(params DiagnosticResult[] diagnostic_results)
        {


            var test = new CSharpSourceGeneratorVerifier<SourceGenerator>.Test
            {

                TestState =
                {
                },

            };


            test.TestState.AdditionalReferences.Add(typeof(PinionCore.Remote.Value<>).Assembly);
            test.TestState.AdditionalReferences.Add(typeof(PinionCore.Remote.Property<>).Assembly);



            foreach (SyntaxTree syntaxTree in _Sources)
            {

                test.TestState.GeneratedSources.Add((typeof(SourceGenerator), syntaxTree.FilePath, await syntaxTree.GetTextAsync()));
            }

            foreach (SyntaxTree syntaxTree in _Souls)
            {
                test.TestState.Sources.Add(await syntaxTree.GetTextAsync());
            }




            await test.RunAsync();
        }


    }
}
