using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using PinionCore.Remote.Tools.Protocol.Sources.Extensions;

namespace PinionCore.Remote.Tools.Protocol.Sources.Tests
{


    public class SyntaxExtensionsTests
    {
        [Test]
        public void Name()
        {
            var source = @"
namespace N1
{
    namespace N2
    {
        interface IA 
        {    
        }
    }
    
}

";
            Microsoft.CodeAnalysis.SyntaxTree tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            CSharpCompilation com = tree.Compilation();
            Microsoft.CodeAnalysis.INamedTypeSymbol symbol = com.GetSemanticModel(tree).GetDeclaredSymbol(tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            InterfaceDeclarationSyntax syntax = symbol.ToInferredInterface();
            InterfaceDeclarationSyntax type = syntax;

            NUnit.Framework.Assert.AreEqual("IA", type.Identifier.ToFullString());
        }

        [Test]
        public void Namespace()
        {
            var source = @"
namespace N1
{
    namespace N2
    {
        interface IA 
        {    
        }
    }
    
}

";
            Microsoft.CodeAnalysis.SyntaxTree tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            CSharpCompilation com = tree.Compilation();
            Microsoft.CodeAnalysis.INamedTypeSymbol symbol = com.GetSemanticModel(tree).GetDeclaredSymbol(tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());


            NamespaceDeclarationSyntax syntax = symbol.ToInferredInterface().Ancestors().OfType<NamespaceDeclarationSyntax>().Single();

            NUnit.Framework.Assert.AreEqual("N1.N2", syntax.Name.ToFullString());
        }
        [Test]
        public void InferredFullNameTest()
        {
            var source = @"
namespace N1
{
    namespace N2
    {
        class C1
        {
            class C2
            {
                interface IA 
                {    
                }
            }
            
        }
        
    }
    
}

";
            Microsoft.CodeAnalysis.SyntaxTree tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            InterfaceDeclarationSyntax type = tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single();

            NUnit.Framework.Assert.AreEqual("N1.N2.C1.C2.IA", type.GetNamePath());

        }


        [Test]
        public void NamespaceNoName()
        {
            var source = @"
interface IA 
        {    
        }
";
            Microsoft.CodeAnalysis.SyntaxTree tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            CSharpCompilation com = tree.Compilation();
            Microsoft.CodeAnalysis.INamedTypeSymbol symbol = com.GetSemanticModel(tree).GetDeclaredSymbol(tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());


            var syntax = symbol.ToInferredInterface().DescendantNodes().OfType<NamespaceDeclarationSyntax>().Any();

            NUnit.Framework.Assert.AreEqual(false, syntax);
        }


        [Test]
        public void MethodIdentifier()
        {
            var source = @"

interface IA {
    void Method1();
}
";
            Microsoft.CodeAnalysis.SyntaxTree tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            CSharpCompilation com = tree.Compilation();
            Microsoft.CodeAnalysis.INamedTypeSymbol symbol = com.GetSemanticModel(tree).GetDeclaredSymbol(tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            InterfaceDeclarationSyntax syntax = symbol.ToInferredInterface();
            MethodDeclarationSyntax method = syntax.DescendantNodes().OfType<MethodDeclarationSyntax>().Single();

            NUnit.Framework.Assert.AreEqual("Method1", method.Identifier.ToString());
        }

        [Test]
        public void IgnoreEventMethod()
        {
            var source = @"

interface IA {
    event System.Action Event;
}
";
            Microsoft.CodeAnalysis.SyntaxTree tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            CSharpCompilation com = tree.Compilation();
            Microsoft.CodeAnalysis.INamedTypeSymbol symbol = com.GetSemanticModel(tree).GetDeclaredSymbol(tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            InterfaceDeclarationSyntax syntax = symbol.ToInferredInterface();
            var count = syntax.DescendantNodes().OfType<MethodDeclarationSyntax>().Count();

            NUnit.Framework.Assert.AreEqual(0, count);
        }




        [Test]
        public void MethodParam()
        {
            var source = @"

interface IA {
    void Method1(int p1);
}
";
            Microsoft.CodeAnalysis.SyntaxTree tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);

            CSharpCompilation com = tree.Compilation();
            Microsoft.CodeAnalysis.INamedTypeSymbol symbol = com.GetSemanticModel(tree).GetDeclaredSymbol(tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            InterfaceDeclarationSyntax syntax = symbol.ToInferredInterface();

            ParameterSyntax parameter = syntax.DescendantNodes().OfType<ParameterSyntax>().Single();

            NUnit.Framework.Assert.AreEqual("p1", parameter.Identifier.ToString());
            NUnit.Framework.Assert.AreEqual("System.Int32", parameter.Type.ToFullString());

        }

        [Test]
        public void MethodNamePathParam()
        {
            var source = @"
using System;
interface IA {
    void Method1(ICloneable p1);
}
";
            Microsoft.CodeAnalysis.SyntaxTree tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);

            CSharpCompilation com = tree.Compilation();
            Microsoft.CodeAnalysis.INamedTypeSymbol symbol = com.GetSemanticModel(tree).GetDeclaredSymbol(tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            InterfaceDeclarationSyntax syntax = symbol.ToInferredInterface();

            ParameterSyntax parameter = syntax.DescendantNodes().OfType<ParameterSyntax>().Single();

            NUnit.Framework.Assert.AreEqual("p1", parameter.Identifier.ToString());
            NUnit.Framework.Assert.AreEqual("System.ICloneable", parameter.Type.ToFullString());

        }

        [Test]
        public void MethodReturn()
        {
            var source = @"

using System;

interface IA {
    IDisposable Method1(int p1);
}
";

            Microsoft.CodeAnalysis.SyntaxTree tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            CSharpCompilation com = tree.Compilation();
            Microsoft.CodeAnalysis.INamedTypeSymbol symbol = com.GetSemanticModel(tree).GetDeclaredSymbol(tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            InterfaceDeclarationSyntax syntax = symbol.ToInferredInterface();
            MethodDeclarationSyntax method = syntax.DescendantNodes().OfType<MethodDeclarationSyntax>().Single();

            NUnit.Framework.Assert.AreEqual("System.IDisposable", method.ReturnType.ToFullString());
        }

        [Test]
        public void MethodReturnSystem()
        {
            var source = @"

using System;

interface IA {
    Tuple<Guid,int> Method1(int p1);
}
";

            Microsoft.CodeAnalysis.SyntaxTree tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            CSharpCompilation com = tree.Compilation();
            Microsoft.CodeAnalysis.INamedTypeSymbol symbol = com.GetSemanticModel(tree).GetDeclaredSymbol(tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            InterfaceDeclarationSyntax syntax = symbol.ToInferredInterface();
            MethodDeclarationSyntax method = syntax.DescendantNodes().OfType<MethodDeclarationSyntax>().Single();

            NUnit.Framework.Assert.AreEqual("System.Tuple<System.Guid, System.Int32>", method.ReturnType.ToFullString());
        }

        [Test]
        public void MethodReturnPinionCoreRemoteValue()
        {
            var source = @"

using System;
using PinionCore.Remote;
interface IA {
    Value<int> Method1(int p1);
}
";

            Microsoft.CodeAnalysis.SyntaxTree tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            CSharpCompilation com = tree.Compilation();


            Microsoft.CodeAnalysis.INamedTypeSymbol symbol = com.GetSemanticModel(tree).GetDeclaredSymbol(com.SyntaxTrees[0].GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            InterfaceDeclarationSyntax syntax = symbol.ToInferredInterface();
            MethodDeclarationSyntax method = syntax.DescendantNodes().OfType<MethodDeclarationSyntax>().Single();

            NUnit.Framework.Assert.AreEqual("PinionCore.Remote.Value<System.Int32>", method.ReturnType.ToFullString());
        }

        [Test]
        public void MethodRefParam()
        {
            var source = @"

using System;

interface IA {
    IDisposable Method1(ref IDisposable p1);
}
";

            Microsoft.CodeAnalysis.SyntaxTree tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            CSharpCompilation com = tree.Compilation();
            Microsoft.CodeAnalysis.INamedTypeSymbol symbol = com.GetSemanticModel(tree).GetDeclaredSymbol(tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            InterfaceDeclarationSyntax syntax = symbol.ToInferredInterface();

            ParameterSyntax parameter = syntax.DescendantNodes().OfType<ParameterSyntax>().Single();

            NUnit.Framework.Assert.AreEqual("ref", parameter.Modifiers[0].Text);

            NUnit.Framework.Assert.AreEqual("p1", parameter.Identifier.ToString());

        }




        [Test]
        public void PropertyIdentifier()
        {
            var source = @"
interface IA {
    int Property1 {get ;};
}
";


            Microsoft.CodeAnalysis.SyntaxTree tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            CSharpCompilation com = tree.Compilation();

            Microsoft.CodeAnalysis.INamedTypeSymbol symbol = com.GetSemanticModel(tree).GetDeclaredSymbol(tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            InterfaceDeclarationSyntax syntax = symbol.ToInferredInterface();
            PropertyDeclarationSyntax member = syntax.DescendantNodes().OfType<PropertyDeclarationSyntax>().Single();

            AccessorDeclarationSyntax accessor = member.DescendantNodes().OfType<AccessorDeclarationSyntax>().Single();



            NUnit.Framework.Assert.AreEqual("Property1", member.Identifier.ToFullString());
            NUnit.Framework.Assert.AreEqual(SyntaxKind.GetAccessorDeclaration, accessor.Kind());
        }

        [Test]
        public void Event()
        {

            var source = @"
using System;
namespace N1
{
    public delegate IDisposable TestDelegate(int a);
}

interface IA
{
    event N1.TestDelegate Event1;
}   
";
            Microsoft.CodeAnalysis.SyntaxTree tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            CSharpCompilation com = tree.Compilation();

            Microsoft.CodeAnalysis.INamedTypeSymbol symbol = com.GetSemanticModel(tree).GetDeclaredSymbol(tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            InterfaceDeclarationSyntax syntax = symbol.ToInferredInterface();

            EventFieldDeclarationSyntax member = syntax.DescendantNodes().OfType<EventFieldDeclarationSyntax>().Single();

            NUnit.Framework.Assert.AreEqual("Event1", member.Declaration.Variables[0].Identifier.ToFullString());

            NUnit.Framework.Assert.AreEqual("N1.TestDelegate", member.Declaration.Type.ToFullString());

        }

        [Test]
        public void EventSystemAction()
        {

            var source = @"
using System;


interface IA
{
    event System.Action<ICloneable> Event1;
}   
";
            Microsoft.CodeAnalysis.SyntaxTree tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            CSharpCompilation com = tree.Compilation();

            Microsoft.CodeAnalysis.INamedTypeSymbol symbol = com.GetSemanticModel(tree).GetDeclaredSymbol(tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            InterfaceDeclarationSyntax syntax = symbol.ToInferredInterface();

            EventFieldDeclarationSyntax member = syntax.DescendantNodes().OfType<EventFieldDeclarationSyntax>().Single();

            NUnit.Framework.Assert.AreEqual("Event1", member.Declaration.Variables[0].Identifier.ToFullString());

            NUnit.Framework.Assert.AreEqual("System.Action<System.ICloneable>", member.Declaration.Type.ToFullString());

        }

        [Test]
        public void Index()
        {

            var source = @"
    interface IA
    {
        string this[int index]
        {
            get;
            set;
        }
    }
}   
";
            Microsoft.CodeAnalysis.SyntaxTree tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            Microsoft.CodeAnalysis.SyntaxNode[] nodes = tree.GetRoot().DescendantNodes().ToArray();
            CSharpCompilation com = tree.Compilation();



            Microsoft.CodeAnalysis.INamedTypeSymbol symbol = com.GetSemanticModel(tree).GetDeclaredSymbol(tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            InterfaceDeclarationSyntax syntax = symbol.ToInferredInterface();

            IndexerDeclarationSyntax member = syntax.DescendantNodes().OfType<IndexerDeclarationSyntax>().Single();
            NUnit.Framework.Assert.AreEqual("System.String", member.Type.ToFullString());




        }


    }





}
