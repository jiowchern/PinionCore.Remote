using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace PinionCore.Remote.Tools.Protocol.Sources.Tests
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NUnit.Framework;

    public class MemberIdProiderTests
    {
        [Test]
        public void Test2()
        {
            var soulSource = @"
namespace PinionCore.Remote
{
    public interface ISoul
    {
        event System.Action<int> Event01;
        PinionCore.Remote.Value<int> GetValue0(int _1, string _2, float _3, double _4, decimal _5, System.Guid _6);

    }
}

    class CPinionCore_Remote_ISoul : PinionCore.Remote.ISoul
    {
       event System.Action<int> PinionCore.Remote.ISoul.Event01
        {
                add { throw new System.NotImplementedException(""Event add accessor not implemented.""); }
                remove { throw new System.NotImplementedException(""Event remove accessor not implemented.""); }
        }

       PinionCore.Remote.Value<int> PinionCore.Remote.ISoul.GetValue0(int _1, string _2, float _3, double _4, decimal _5, System.Guid _6)
        {
            throw new System.NotImplementedException(""Method not implemented."");
        }
    }
";

     
            Microsoft.CodeAnalysis.SyntaxTree tree = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseSyntaxTree(SourceText.From(soulSource));

            var souls = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InterfaceDeclarationSyntax>().ToArray();
            var ghosts = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>().ToArray();


            var memberIdProvider = new MemberIdProvider(souls);

            foreach(var ghost in ghosts)
            {
                var eventMember = ghost.DescendantNodes().OfType<EventDeclarationSyntax>();
                var eventId = memberIdProvider.DisrbutionIdWithGhost(eventMember.First());

                NUnit.Framework.Assert.Greater(eventId, 0);

                var methodMember = ghost.DescendantNodes().OfType<MethodDeclarationSyntax>();
                var methodId = memberIdProvider.DisrbutionIdWithGhost(methodMember.First());
                NUnit.Framework.Assert.Greater(methodId, 0);
            }

        }
        [Test]
        public void Test1()
        {
            var source = @"
namespace PinionCore.Remote.Tools.Protocol.Sources.TestCommon
{
    public delegate void CustomDelegate();
    public interface IEventabe : IEventabe2
    {
        event System.Action<int, string, float, double, decimal, System.Guid> Event01;

        event System.Action Event02;

        event CustomDelegate CustomDelegateEvent;
    }

    public interface IEventabe1Test
    {
        event System.Action Event1;
        event System.Action Event2;
        event System.Action Event23;

    }
    public interface IEventabe1
    {
        event System.Action Event1;
        event System.Action<int> Event2;

        
    }

    public interface IEventabe2 : IEventabe1
    {
        event System.Action Event21;
        event System.Action<int> Event22;
    }

    public interface IMethodable : IMethodable2
    {
        PinionCore.Remote.Value<int[]> GetValue0(int _1, string _2, float _3, double _4, decimal _5, System.Guid _6);

        int NotSupported();

        PinionCore.Remote.Value<IMethodable> GetValueSelf();
    }

    namespace PinionCore.Remote.Tools.Protocol.Sources.TestCommon
    {
        public interface IMethodable1
        {
            PinionCore.Remote.Value<int> GetValue1();
        }
    }

    public class HelloRequest
    {
        public string Name;
    }

    public class HelloReply
    {
        public string Message;
    }


    public interface IMethodable2 : IMethodable1
    {
        PinionCore.Remote.Value<int> GetValue2();
        PinionCore.Remote.Value<HelloReply> SayHello(HelloRequest request);
    }
    namespace MultipleNotices
    {
        public interface IMultipleNotices
        {
            PinionCore.Remote.Value<int> GetNumber1Count();
            PinionCore.Remote.Value<int> GetNumber2Count();
            PinionCore.Remote.Notifier<INumber> Numbers1 { get; }
            PinionCore.Remote.Notifier<INumber> Numbers2 { get; }
        }
    }
    public class TestC
    {
        public string F1;
    }
    public interface ITest
    {
        void M1(TestC a1, TestS a2);
    }
    public struct TestS
    {
        public string F1;
    }

    public interface INext
    {
        Remote.Value<bool> Next();
    }
    public interface INumber
    {
        Property<int> Value { get; }
    }
    public interface IPropertyable : IPropertyable2
    {

    }
    public interface IPropertyable1
    {
        PinionCore.Remote.Property<int> Property1 { get; }
    }
    public interface IPropertyable2 : IPropertyable1
    {
        PinionCore.Remote.Property<int> Property2 { get; }
    }

    public interface ITagable
    {

    }
}

";
            Microsoft.CodeAnalysis.SyntaxTree tree = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseSyntaxTree(SourceText.From(source));

            InterfaceDeclarationSyntax[] gpis = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InterfaceDeclarationSyntax>().ToArray();

            var memberIdProvider = new MemberIdProvider(gpis);


            InterfaceInheritor[] interfaceInheritors = gpis.Select(g => new InterfaceInheritor(g)).ToArray();

            var classes = new System.Collections.Generic.List<ClassDeclarationSyntax>();
            foreach (InterfaceInheritor interfaceInheritor in interfaceInheritors)
            {
                ClassDeclarationSyntax type = SyntaxFactory.ClassDeclaration(interfaceInheritor.Base.Identifier.Text + "Impl");
                type = interfaceInheritor.Inherite(type);
                classes.Add(type);
            }

            var eventIdCount = new Dictionary<int, int>();
            IEnumerable<EventDeclarationSyntax> classEvents = classes.SelectMany(c => c.DescendantNodes().OfType<EventDeclarationSyntax>());
            foreach (EventDeclarationSyntax classEvent in classEvents)
            {
                var id = memberIdProvider.DisrbutionIdWithGhost(classEvent);
                if (!eventIdCount.ContainsKey(id))
                    eventIdCount[id] = 0;
                eventIdCount[id]++;
                Assert.Greater(id, 0);
            }

            var memberCodeBuilder = new MemberMapCodeBuilder(gpis, memberIdProvider);
            IEnumerable<System.Tuple<InterfaceDeclarationSyntax, EventFieldDeclarationSyntax>> events = memberCodeBuilder.GetMembers<EventFieldDeclarationSyntax>(gpis);
            foreach (System.Tuple<InterfaceDeclarationSyntax, EventFieldDeclarationSyntax> @event in events)
            {
                var id = memberIdProvider.GetIdWithSoul(@event.Item2);
                eventIdCount[id]--;
                Assert.Greater(id, 0);
            }


            foreach (KeyValuePair<int, int> idcount in eventIdCount)
            {
                NUnit.Framework.Assert.AreEqual(0, idcount.Value);
            }
        }
    }

}
