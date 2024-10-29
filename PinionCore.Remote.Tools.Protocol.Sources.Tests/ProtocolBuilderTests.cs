using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace PinionCore.Remote.Tools.Protocol.Sources.Tests
{
    public class ProtocolBuilderTests
    {


        [Test]
        public async Task ProtocolTest()
        {
            var source = @"
public interface IA
{
    
}
namespace NS1
{
    
    public class C1{}
    public interface IB
    {
       
    }
}

";
            Microsoft.CodeAnalysis.SyntaxTree tree = CSharpSyntaxTree.ParseText(source);


            await new GhostTest(tree).RunAsync();
        }

    }


}
