using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace PinionCore.Remote.Tools.Protocol.Sources.Tests
{    
    public class SyntaxModifierTests
    {
        
        [Test]
        public void ZeroProperty()
        {
            var source = @"
interface IA {
    System.Int32 Property1 {get;}
    PinionCore.Remote.Property<System.Int32> Property2 {set;}
}
";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            var com = tree.Compilation();
            var root = com.SyntaxTrees[0].GetRoot();
            var builder = new PinionCore.Remote.Tools.Protocol.Sources.InterfaceInheritor(root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            var cia = SyntaxFactory.ClassDeclaration("CIA");
            cia = builder.Inherite(cia);

            var modifier = SyntaxModifier.Create(com).Mod(cia);
            

            var exps = modifier.Type.DescendantNodes().OfType<BlockSyntax>().ToArray();
            NUnit.Framework.Assert.AreEqual(0, modifier.TypesOfSerialization.Count());
            NUnit.Framework.Assert.True(builder.Expression.IsEquivalentTo(exps[0]));
            NUnit.Framework.Assert.True(builder.Expression.IsEquivalentTo(exps[1]));
            NUnit.Framework.Assert.AreEqual(0, modifier.Type.DescendantNodes().OfType<FieldDeclarationSyntax>().Count());


        }


        [Test]
        public void PropertyPropertyTrue()
        {
            var source = @"
interface IA {
    PinionCore.Remote.Property<System.Int32> Property1 {get;}
}
";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            var com = tree.Compilation();
            var root = com.SyntaxTrees[0].GetRoot();
            var builder = new PinionCore.Remote.Tools.Protocol.Sources.InterfaceInheritor(root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            var cia = SyntaxFactory.ClassDeclaration("CIA");
            cia = builder.Inherite(cia);

            var modifier = SyntaxModifier.Create(com).Mod(cia);

            var exp = modifier.Type.DescendantNodes().OfType<BlockSyntax>().Single();
            NUnit.Framework.Assert.AreEqual(1, modifier.TypesOfSerialization.Count());
            NUnit.Framework.Assert.False(builder.Expression.IsEquivalentTo(exp));
            NUnit.Framework.Assert.AreEqual(1, modifier.Type.DescendantNodes().OfType<FieldDeclarationSyntax>().Count());


        }

        [Test]
        public void PropertyPropertyFail()
        {
            var source = @"
interface IA {
    PinionCore.Remote.Property<IA> Property1 {get;}
}
";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            var com = tree.Compilation();
            var root = com.SyntaxTrees[0].GetRoot();
            var builder = new PinionCore.Remote.Tools.Protocol.Sources.InterfaceInheritor(root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            var cia = SyntaxFactory.ClassDeclaration("CIA");
            cia = builder.Inherite(cia);

            var modifier = SyntaxModifier.Create(com).Mod(cia);

            var exp = modifier.Type.DescendantNodes().OfType<BlockSyntax>().Single();
            NUnit.Framework.Assert.AreEqual(0, modifier.TypesOfSerialization.Count());
            NUnit.Framework.Assert.True(builder.Expression.IsEquivalentTo(exp));
            NUnit.Framework.Assert.AreEqual(0, modifier.Type.DescendantNodes().OfType<FieldDeclarationSyntax>().Count());


        }

        [Test]
        public void PropertyNotifierFail()
        {
            var source = @"
interface IA {
    PinionCore.Remote.Notifier<System.Int32> Property1 {get;}
}
";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            var com = tree.Compilation();
            var root = com.SyntaxTrees[0].GetRoot();
            var builder = new PinionCore.Remote.Tools.Protocol.Sources.InterfaceInheritor(root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            var cia = SyntaxFactory.ClassDeclaration("CIA");
            cia = builder.Inherite(cia);

            var modifier = SyntaxModifier.Create(com).Mod(cia);

            var exp = modifier.Type.DescendantNodes().OfType<BlockSyntax>().Single();
            NUnit.Framework.Assert.AreEqual(0, modifier.TypesOfSerialization.Count());
            NUnit.Framework.Assert.True(builder.Expression.IsEquivalentTo(exp));
            NUnit.Framework.Assert.AreEqual(0, modifier.Type.DescendantNodes().OfType<FieldDeclarationSyntax>().Count());


        }

        [Test]
        public void PropertyNotifierSuccess()
        {
            var source = @"
interface IA {
    PinionCore.Remote.Notifier<IA> Property1 {get;}
}
";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            var com = tree.Compilation();
            var root = com.SyntaxTrees[0].GetRoot();
            var builder = new PinionCore.Remote.Tools.Protocol.Sources.InterfaceInheritor(root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            var cia = SyntaxFactory.ClassDeclaration("CIA");
            cia = builder.Inherite(cia);

            var modifier = SyntaxModifier.Create(com).Mod(cia);

            var exp = modifier.Type.DescendantNodes().OfType<BlockSyntax>().Single();
            NUnit.Framework.Assert.AreEqual(0, modifier.TypesOfSerialization.Count());
            NUnit.Framework.Assert.False(builder.Expression.IsEquivalentTo(exp));
            NUnit.Framework.Assert.AreEqual(1, modifier.Type.DescendantNodes().OfType<FieldDeclarationSyntax>().Count());


        }


        [Test]
        public void ZeroEvent()
        {
            var source = @"
interface IA {
    event System.Func<int> Event1;
}
";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            var com = tree.Compilation();
            var root = com.SyntaxTrees[0].GetRoot();
            var builder = new PinionCore.Remote.Tools.Protocol.Sources.InterfaceInheritor(root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            var cia = SyntaxFactory.ClassDeclaration("CIA");
            cia = builder.Inherite(cia);

            var modifier = SyntaxModifier.Create(com).Mod(cia);

            var exps = modifier.Type.DescendantNodes().OfType<BlockSyntax>().ToArray();
            NUnit.Framework.Assert.AreEqual(0, modifier.TypesOfSerialization.Count());
            NUnit.Framework.Assert.True(builder.Expression.IsEquivalentTo(exps[0]));
            NUnit.Framework.Assert.True(builder.Expression.IsEquivalentTo(exps[1]));
        }

        [Test]
        public void EventActionInterface()
        {
            var source = @"
interface IA {
    event System.Action<IA> Event1;
}
";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            var com = tree.Compilation();
            var root = com.SyntaxTrees[0].GetRoot();
            var builder = new PinionCore.Remote.Tools.Protocol.Sources.InterfaceInheritor(root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            var cia = SyntaxFactory.ClassDeclaration("CIA");
            cia = builder.Inherite(cia);

            var modifier = SyntaxModifier.Create(com).Mod(cia);

            var exps = modifier.Type.DescendantNodes().OfType<BlockSyntax>().ToArray();
            NUnit.Framework.Assert.AreEqual(0, modifier.TypesOfSerialization.Count());
            NUnit.Framework.Assert.True(builder.Expression.IsEquivalentTo(exps[0]));
            NUnit.Framework.Assert.True(builder.Expression.IsEquivalentTo(exps[1]));

        }


        [Test]
        public void EventActionInt()
        {
            var source = @"
interface IA {
    event System.Action<System.Int32> Event1;
}
";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            var com = tree.Compilation();
            var root = com.SyntaxTrees[0].GetRoot();
            var builder = new PinionCore.Remote.Tools.Protocol.Sources.InterfaceInheritor(root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            var cia = SyntaxFactory.ClassDeclaration("CIA");
            cia = builder.Inherite(cia);

            var modifier = SyntaxModifier.Create(com).Mod(cia);

            var exps = modifier.Type.DescendantNodes().OfType<BlockSyntax>().ToArray();
            NUnit.Framework.Assert.AreEqual(1, modifier.TypesOfSerialization.Count());
            NUnit.Framework.Assert.False(builder.Expression.IsEquivalentTo(exps[0]));
            NUnit.Framework.Assert.False(builder.Expression.IsEquivalentTo(exps[1]));

        }

        [Test]
        public void EventActionIntArray()
        {
            var source = @"
interface IA {
    event System.Action<System.Int32[]> Event1;
}
";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            var com = tree.Compilation();
            var root = com.SyntaxTrees[0].GetRoot();
            var builder = new PinionCore.Remote.Tools.Protocol.Sources.InterfaceInheritor(root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            var cia = SyntaxFactory.ClassDeclaration("CIA");
            cia = builder.Inherite(cia);

            var modifier = SyntaxModifier.Create(com).Mod(cia);

            var exps = modifier.Type.DescendantNodes().OfType<BlockSyntax>().ToArray();
            NUnit.Framework.Assert.AreEqual(1, modifier.TypesOfSerialization.Count());
            NUnit.Framework.Assert.False(builder.Expression.IsEquivalentTo(exps[0]));
            NUnit.Framework.Assert.False(builder.Expression.IsEquivalentTo(exps[1]));

        }
        [Test]
        public void EventAction()
        {
            var source = @"
interface IA {
    event System.Action Event1;
}
";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            var com = tree.Compilation();
            var root = com.SyntaxTrees[0].GetRoot();
            var builder = new PinionCore.Remote.Tools.Protocol.Sources.InterfaceInheritor(root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            var cia = SyntaxFactory.ClassDeclaration("CIA");
            cia = builder.Inherite(cia);

            var modifier = SyntaxModifier.Create(com).Mod(cia);

            var exps = modifier.Type.DescendantNodes().OfType<BlockSyntax>().ToArray();
            NUnit.Framework.Assert.AreEqual(0, modifier.TypesOfSerialization.Count());
            NUnit.Framework.Assert.False(builder.Expression.IsEquivalentTo(exps[0]));
            NUnit.Framework.Assert.False(builder.Expression.IsEquivalentTo(exps[1]));

            NUnit.Framework.Assert.AreEqual(1, modifier.Type.DescendantNodes().OfType<FieldDeclarationSyntax>().Count());
            

        }

        [Test]
        public void ZeroMethod()
        {
            var source = @"
interface IA {
    int Method1();
    void Method2(out System.Int32 val);
}
";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            var com = tree.Compilation();
            var root = com.SyntaxTrees[0].GetRoot();
            var builder = new PinionCore.Remote.Tools.Protocol.Sources.InterfaceInheritor(root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            var cia = SyntaxFactory.ClassDeclaration("CIA");
            cia = builder.Inherite(cia);

            var modifier = SyntaxModifier.Create(com).Mod(cia);
            cia = modifier.Type;

            var exps = cia.DescendantNodes().OfType<BlockSyntax>().ToArray();
            NUnit.Framework.Assert.AreEqual(0, modifier.TypesOfSerialization.Count());
            NUnit.Framework.Assert.True(builder.Expression.IsEquivalentTo(exps[0]));
            NUnit.Framework.Assert.True(builder.Expression.IsEquivalentTo(exps[1]));
        }
        [Test]
        public void MethodVoidT()
        {
            var source = @"
interface IA {
    void Method1<T>(T int);
}
";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            var com = tree.Compilation();
            var root = com.SyntaxTrees[0].GetRoot();
            var builder = new PinionCore.Remote.Tools.Protocol.Sources.InterfaceInheritor(root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            var cia = SyntaxFactory.ClassDeclaration("CIA");
            cia = builder.Inherite(cia);

            var modifier = SyntaxModifier.Create(com).Mod(cia);
            cia = modifier.Type;

            var exp = cia.DescendantNodes().OfType<BlockSyntax>().Single();
            NUnit.Framework.Assert.AreEqual(0, modifier.TypesOfSerialization.Count());
            NUnit.Framework.Assert.True(builder.Expression.IsEquivalentTo(exp));
        }
            [Test]
        public void MethodVoid()
        {
            var source = @"
interface IA {
    void Method1();
}
";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            var com = tree.Compilation();
            var root = com.SyntaxTrees[0].GetRoot();
            var builder = new PinionCore.Remote.Tools.Protocol.Sources.InterfaceInheritor(root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            var cia = SyntaxFactory.ClassDeclaration("CIA");
            cia = builder.Inherite(cia);

            var modifier = SyntaxModifier.Create(com).Mod(cia);
            cia = modifier.Type;

            var exp = cia.DescendantNodes().OfType<BlockSyntax>().Single();
            NUnit.Framework.Assert.AreEqual(0, modifier.TypesOfSerialization.Count());            
            NUnit.Framework.Assert.False(builder.Expression.IsEquivalentTo(exp));
        }

        [Test]
        public void MethodInt()
        {
            var source = @"
interface IA {
    int Method1();
}
";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            var com = tree.Compilation();
            var root = com.SyntaxTrees[0].GetRoot();
            var builder = new PinionCore.Remote.Tools.Protocol.Sources.InterfaceInheritor(root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            var cia = SyntaxFactory.ClassDeclaration("CIA");
            cia = builder.Inherite(cia);

            var modifier = SyntaxModifier.Create(com).Mod(cia); 
            cia = modifier.Type;

            var exp = cia.DescendantNodes().OfType<BlockSyntax>().Single();
            NUnit.Framework.Assert.AreEqual(0, modifier.TypesOfSerialization.Count());
            NUnit.Framework.Assert.True(builder.Expression.IsEquivalentTo(exp));
        }

        

        [Test]
        public void MethodValue()
        {
            var source = @"

interface IA {
    PinionCore.Remote.Value<int> Method1();
}
";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            var com = tree.Compilation();
            var root = com.SyntaxTrees[0].GetRoot();
            var builder = new PinionCore.Remote.Tools.Protocol.Sources.InterfaceInheritor(root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            var cia = SyntaxFactory.ClassDeclaration("CIA");
            cia = builder.Inherite(cia);

            var modifier = SyntaxModifier.Create(com).Mod(cia); 
            cia = modifier.Type;

            var exp = cia.DescendantNodes().OfType<BlockSyntax>().Single();
            NUnit.Framework.Assert.AreEqual(1, modifier.TypesOfSerialization.Count());
            NUnit.Framework.Assert.False(builder.Expression.IsEquivalentTo(exp));
        }


        [Test]
        public void MethodValueParamInt()
        {
            var source = @"

interface IA {
    PinionCore.Remote.Value<System.Int32> Method1(System.Int32 i);
}
";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            var com = tree.Compilation();
            var root = com.SyntaxTrees[0].GetRoot();
            var builder = new PinionCore.Remote.Tools.Protocol.Sources.InterfaceInheritor(root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            var cia = SyntaxFactory.ClassDeclaration("CIA");
            cia = builder.Inherite(cia);

            var modifier = SyntaxModifier.Create(com).Mod(cia);
            cia = modifier.Type;

            var exp = cia.DescendantNodes().OfType<BlockSyntax>().Single();
            NUnit.Framework.Assert.AreEqual(2, modifier.TypesOfSerialization.Count());
            NUnit.Framework.Assert.False(builder.Expression.IsEquivalentTo(exp));
        }

        [Test]
        public void MethodValueParamGuid()
        {
            var source = @"

interface IA {
    PinionCore.Remote.Value<System.Int32> Method1(System.Guid i);
}
";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            var com = tree.Compilation();
            var root = com.SyntaxTrees[0].GetRoot();
            var builder = new PinionCore.Remote.Tools.Protocol.Sources.InterfaceInheritor(root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            var cia = SyntaxFactory.ClassDeclaration("CIA");
            cia = builder.Inherite(cia);

            var modifier = SyntaxModifier.Create(com).Mod(cia);
            cia = modifier.Type;

            var exp = cia.DescendantNodes().OfType<BlockSyntax>().Single();
            NUnit.Framework.Assert.AreEqual(2, modifier.TypesOfSerialization.Count());
            NUnit.Framework.Assert.False(builder.Expression.IsEquivalentTo(exp));
        }


        [Test]
        public void MethodVoidParam1()
        {
            var source = @"
interface IA {
    void Method1(System.Int32 i);
}
";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            var com = tree.Compilation();
            var root = com.SyntaxTrees[0].GetRoot();
            var builder = new PinionCore.Remote.Tools.Protocol.Sources.InterfaceInheritor(root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single());

            var cia = SyntaxFactory.ClassDeclaration("CIA");
            cia = builder.Inherite(cia);

            var modifier = SyntaxModifier.Create(com).Mod(cia);
            cia = modifier.Type;

            var exp = cia.DescendantNodes().OfType<BlockSyntax>().Single();
            NUnit.Framework.Assert.AreEqual(1, modifier.TypesOfSerialization.Count());
            NUnit.Framework.Assert.False(builder.Expression.IsEquivalentTo(exp));

            var method  = cia.DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            string paramName = method.ParameterList.Parameters.Single().Identifier.ToString();

            NUnit.Framework.Assert.AreEqual("_1" , paramName);

        }

       

        [Test]
        public void ImplementPinionCoreRemoteIGhost()
        {
            var cia = SyntaxFactory.ClassDeclaration("C123");
            cia = cia.ImplementPinionCoreRemoteIGhost();

            var member = cia.DescendantNodes().OfType<ConstructorDeclarationSyntax>().Single();

            NUnit.Framework.Assert.AreEqual("C123" , member.Identifier.ToString());
            
        }

        [Test]
        public void CreatePinionCoreRemoteIEventProxyCreater()
        {
            var source = @"

class CIA : NS1.IA {
    event System.Action<PinionCore.Int,TT<Test.FLoat>> NS1.IA.Event1
    {
        add{}
        remove{}
    }
}
";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            var efd = tree.GetRoot().DescendantNodes().OfType<EventDeclarationSyntax>().Single();
            var cefd = efd.CreatePinionCoreRemoteIEventProxyCreater();
            NUnit.Framework.Assert.AreEqual("CNS1_IA_Event1", cefd.Identifier.ValueText);

        }

        [Test]
        public void CreatePinionCoreRemoteIEventProxyCreaterNoTypes()
        {
            var source = @"
class CIA : NS1.IA {
    event System.Action NS1.IA.Event1
    {
        add{}
        remove{}
    }
}
";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            var efd = tree.GetRoot().DescendantNodes().OfType<EventDeclarationSyntax>().Single();
            var cefd = efd.CreatePinionCoreRemoteIEventProxyCreater();
            NUnit.Framework.Assert.AreEqual("CNS1_IA_Event1", cefd.Identifier.ValueText);

        }
    }
 
    
}