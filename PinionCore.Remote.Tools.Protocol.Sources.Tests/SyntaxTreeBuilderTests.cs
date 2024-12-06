using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace PinionCore.Remote.Tools.Protocol.Sources.Tests
{
    using NUnit.Framework;
    public class SyntaxTreeBuilderTests
    {
        [Test]
        public void InterfacesTest()
        {
            var source = @"public interface IA {}";
            var syntaxBuilder = new PinionCore.Remote.Tools.Protocol.Sources.SyntaxTreeBuilder(SourceText.From(source));
            System.Collections.Generic.IEnumerable<Microsoft.CodeAnalysis.CSharp.Syntax.InterfaceDeclarationSyntax> interfaces = syntaxBuilder.GetInterfaces("IA");
            Assert.AreEqual(1, interfaces.Count());
        }




    }
    
}
