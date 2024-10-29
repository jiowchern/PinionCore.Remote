using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace PinionCore.Remote.Tools.Protocol.Sources.Tests
{
    public static class HelperExt
    {
        public static CSharpCompilation Compile(IEnumerable<SyntaxTree> trees)
        {
            var assemblyName = Guid.NewGuid().ToString();

            IEnumerable<MetadataReference> references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(PinionCore.Remote.Protocol.CreaterAttribute).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(PinionCore.Remote.Value<>).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(PinionCore.Remote.Property<>).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(PinionCore.Remote.Notifier<>).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Action).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Action<>).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Action<,,,,,,,,>).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Collections, Version=4.1.2.0").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Linq.Expressions, Version=4.2.2.0").Location)
            };


            return CSharpCompilation.Create(assemblyName, trees, references, new CSharpCompilationOptions(outputKind: OutputKind.DynamicallyLinkedLibrary));
        }
        public static CSharpCompilation Compilate(params SyntaxTree[] trees)
        {
            return Compile(trees);
        }

        public static CSharpCompilation Compilation(this SyntaxTree tree)
        {
            var assemblyName = Guid.NewGuid().ToString();

            IEnumerable<MetadataReference> references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(PinionCore.Remote.Protocol.CreaterAttribute).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(PinionCore.Remote.Value<>).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(PinionCore.Remote.Property<>).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(PinionCore.Remote.Notifier<>).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Action).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Action<>).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Action<,,,,,,,,>).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Collections, Version=4.1.2.0").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Linq.Expressions, Version=4.2.2.0").Location)

            };

            return CSharpCompilation.Create(assemblyName, new[] { tree }, references);
        }



        public static System.Reflection.Assembly ToAssembly(this Microsoft.CodeAnalysis.CSharp.CSharpCompilation compilation)
        {

            Assembly assembly = null;
            using (var memoryStream = new System.IO.MemoryStream())
            {
                Microsoft.CodeAnalysis.Emit.EmitResult emitResult = compilation.Emit(memoryStream);
                if (emitResult.Success)
                {
                    memoryStream.Seek(0, System.IO.SeekOrigin.Begin);

                    System.Runtime.Loader.AssemblyLoadContext assemblyLoadContext = System.Runtime.Loader.AssemblyLoadContext.Default;

                    assembly = assemblyLoadContext.Assemblies.FirstOrDefault(a => a.GetName()?.Name == compilation.AssemblyName);
                    if (assembly == null)
                    {
                        assembly = assemblyLoadContext.LoadFromStream(memoryStream);
                    }
                }
            }

            return assembly;
        }
    }
}
