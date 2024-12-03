using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PinionCore.Remote.Tools.Protocol.Sources
{
    public class DialogProvider
    {



        public static readonly DiagnosticDescriptor ExceptionDescriptor = new DiagnosticDescriptor("RRSE1", "Error", "unknown error:{0}", "Execute", DiagnosticSeverity.Error, false, null, "https://github.com/jiowchern/PinionCore.Remote/wiki/RRSE1");

        public static readonly DiagnosticDescriptor UnsupportDescriptor = new DiagnosticDescriptor("RRSW1", "Warning", "Unsupport({0}):{1}", "Execute", DiagnosticSeverity.Warning, false, null, "https://github.com/jiowchern/PinionCore.Remote/wiki/RRSW1");
        public static readonly DiagnosticDescriptor MissingReferenceDescriptor = new DiagnosticDescriptor("RRSW2", "Warning", "Missing essentialt type :{0}", "Execute", DiagnosticSeverity.Warning, false, null, "https://github.com/jiowchern/PinionCore.Remote/wiki/RRSW2");
        public DialogProvider()
        {




        }

        internal System.Collections.Generic.IEnumerable<Diagnostic> Unsupports(IEnumerable<ModResult> classAndTypess)
        {
            foreach (ModResult cnt in classAndTypess)
            {
                MethodDeclarationSyntax[] methods = cnt.GetSyntaxs<MethodDeclarationSyntax>().ToArray();
                IndexerDeclarationSyntax[] indexs = cnt.GetSyntaxs<IndexerDeclarationSyntax>().ToArray();
                EventDeclarationSyntax[] events = cnt.GetSyntaxs<EventDeclarationSyntax>().ToArray();
                PropertyDeclarationSyntax[] propertys = cnt.GetSyntaxs<PropertyDeclarationSyntax>().ToArray();

                foreach (IndexerDeclarationSyntax item in indexs)
                {
                    yield return _Unsupport(item.WithAccessorList(Microsoft.CodeAnalysis.CSharp.SyntaxFactory.AccessorList()), "index");
                }
                foreach (MethodDeclarationSyntax item in methods)
                {
                    yield return _Unsupport(item.WithBody(Microsoft.CodeAnalysis.CSharp.SyntaxFactory.Block()), "method");
                }
                foreach (EventDeclarationSyntax item in events)
                {
                    yield return _Unsupport(item.WithAccessorList(Microsoft.CodeAnalysis.CSharp.SyntaxFactory.AccessorList()), "event");
                }
                foreach (PropertyDeclarationSyntax item in propertys)
                {
                    yield return _Unsupport(item.WithAccessorList(Microsoft.CodeAnalysis.CSharp.SyntaxFactory.AccessorList()), "property");
                }


            }

        }

        private Diagnostic _Unsupport(SyntaxNode node, string type)
        {
            return Diagnostic.Create(UnsupportDescriptor, Location.None, type, node.NormalizeWhitespace().ToFullString());
        }

        public Diagnostic Exception(string msg)
        {
            return Diagnostic.Create(ExceptionDescriptor, Location.None, msg);

        }

        internal Diagnostic MissingReference(MissingTypeException e)
        {
            return Diagnostic.Create(MissingReferenceDescriptor, Location.None, e.ToString());
        }


    }
}
