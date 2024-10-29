using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace PinionCore.Remote.Tools.Protocol.Sources.Tests
{
    public class InterfaceInheritorTests
    {

        [Test]
        public void Method1()
        {
            var source = @"

interface IA {
    void Method1();
}
";

            Microsoft.CodeAnalysis.SyntaxTree tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);

            var builder = new PinionCore.Remote.Tools.Protocol.Sources.InterfaceInheritor(tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());


            ClassDeclarationSyntax cia = SyntaxFactory.ClassDeclaration("CIA");
            cia = builder.Inherite(cia);
            MethodDeclarationSyntax member = cia.DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            NUnit.Framework.Assert.AreEqual("IA", member.ExplicitInterfaceSpecifier.Name.ToFullString());

            BlockSyntax exp = member.DescendantNodes().OfType<BlockSyntax>().Single();
            NUnit.Framework.Assert.True(builder.Expression.IsEquivalentTo(exp));
        }

        [Test]
        public void NamespaceMethod()
        {
            var source = @"
namespace N2
{
    namespace N1
    {
        interface IA {
            void Method1();
        }
    }
}


";
            Microsoft.CodeAnalysis.SyntaxTree tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);

            var builder = new PinionCore.Remote.Tools.Protocol.Sources.InterfaceInheritor(tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            ClassDeclarationSyntax cia = SyntaxFactory.ClassDeclaration("CIA");
            cia = builder.Inherite(cia);
            MethodDeclarationSyntax member = cia.DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            NUnit.Framework.Assert.AreEqual("N2.N1.IA", member.ExplicitInterfaceSpecifier.Name.ToFullString());

            BlockSyntax exp = member.DescendantNodes().OfType<BlockSyntax>().Single();
            NUnit.Framework.Assert.True(builder.Expression.IsEquivalentTo(exp));
        }

        [Test]
        public void Index1()
        {
            var source = @"

interface IA {
    string this[int index]
        {
            get;
            set;
        }
}
";
            Microsoft.CodeAnalysis.SyntaxTree tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);

            var builder = new PinionCore.Remote.Tools.Protocol.Sources.InterfaceInheritor(tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            ClassDeclarationSyntax cia = SyntaxFactory.ClassDeclaration("CIA");
            cia = builder.Inherite(cia);
            IndexerDeclarationSyntax member = cia.DescendantNodes().OfType<IndexerDeclarationSyntax>().Single();

            NUnit.Framework.Assert.AreEqual("IA", member.ExplicitInterfaceSpecifier.Name.ToFullString());

            BlockSyntax[] exps = member.DescendantNodes().OfType<BlockSyntax>().ToArray();
            NUnit.Framework.Assert.True(builder.Expression.IsEquivalentTo(exps[0]));
            NUnit.Framework.Assert.True(builder.Expression.IsEquivalentTo(exps[1]));

        }

        [Test]
        public void Property1()
        {
            var source = @"

interface IA {
    int Property1 {get; set;}
}
";
            Microsoft.CodeAnalysis.SyntaxTree tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);

            var builder = new PinionCore.Remote.Tools.Protocol.Sources.InterfaceInheritor(tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());
            ClassDeclarationSyntax cia = SyntaxFactory.ClassDeclaration("CIA");
            cia = builder.Inherite(cia);
            PropertyDeclarationSyntax member = cia.DescendantNodes().OfType<PropertyDeclarationSyntax>().Single();

            NUnit.Framework.Assert.AreEqual("IA", member.ExplicitInterfaceSpecifier.Name.ToFullString());
            BlockSyntax[] exps = member.DescendantNodes().OfType<BlockSyntax>().ToArray();
            NUnit.Framework.Assert.True(builder.Expression.IsEquivalentTo(exps[0]));
            NUnit.Framework.Assert.True(builder.Expression.IsEquivalentTo(exps[1]));
        }

        [Test]
        public void Base1()
        {
            var source = @"

interface IA {    
}
";
            Microsoft.CodeAnalysis.SyntaxTree tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);


            var builder = new PinionCore.Remote.Tools.Protocol.Sources.InterfaceInheritor(tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());
            ClassDeclarationSyntax cia = SyntaxFactory.ClassDeclaration("CIA");
            cia = builder.Inherite(cia);

            BaseTypeSyntax base1 = cia.BaseList.Types[0];
            NUnit.Framework.Assert.AreEqual("IA", base1.ToFullString());
        }




        [Test]
        public void Property2()
        {
            var source = @"

interface IA {
    int Property1 {get; }
}
";
            Microsoft.CodeAnalysis.SyntaxTree tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);


            var builder = new PinionCore.Remote.Tools.Protocol.Sources.InterfaceInheritor(tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());
            ClassDeclarationSyntax cia = SyntaxFactory.ClassDeclaration("CIA");
            cia = builder.Inherite(cia);
            PropertyDeclarationSyntax member = cia.DescendantNodes().OfType<PropertyDeclarationSyntax>().Single();

            NUnit.Framework.Assert.AreEqual("IA", member.ExplicitInterfaceSpecifier.Name.ToFullString());
            BlockSyntax exp = member.DescendantNodes().OfType<BlockSyntax>().Single();
            NUnit.Framework.Assert.True(builder.Expression.IsEquivalentTo(exp));


        }

        [Test]
        public void Event1()
        {
            var source = @"
interface IA {
    event System.Action<int> Event1;
}
";
            Microsoft.CodeAnalysis.SyntaxTree tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);


            var builder = new PinionCore.Remote.Tools.Protocol.Sources.InterfaceInheritor(tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            ClassDeclarationSyntax cia = SyntaxFactory.ClassDeclaration("CIA");
            cia = builder.Inherite(cia);
            EventDeclarationSyntax member = cia.DescendantNodes().OfType<EventDeclarationSyntax>().Single();
            NUnit.Framework.Assert.AreEqual("IA", member.ExplicitInterfaceSpecifier.Name.ToFullString());
            NUnit.Framework.Assert.AreEqual("System.Action<int> ", member.Type.ToFullString());

            BlockSyntax[] exps = member.DescendantNodes().OfType<BlockSyntax>().ToArray();
            NUnit.Framework.Assert.True(builder.Expression.IsEquivalentTo(exps[0]));
            NUnit.Framework.Assert.True(builder.Expression.IsEquivalentTo(exps[1]));

        }


    }
}
