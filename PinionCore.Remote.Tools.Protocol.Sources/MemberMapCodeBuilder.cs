using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;




namespace PinionCore.Remote.Tools.Protocol.Sources
{

    using System.Linq;
    using PinionCore.Remote.Tools.Protocol.Sources.Extensions;

    class MemberMapCodeBuilder
    {

        public readonly string MethodInfosCode;
        public readonly string EventInfosCode;
        public readonly string PropertyInfosCode;
        public readonly string InterfacesCode;
        public MemberMapCodeBuilder(IEnumerable<InterfaceDeclarationSyntax> _interfaces)
        {
            IEnumerable<string> methods = from interfaceSyntax in _interfaces
                                          from methodSyntax in interfaceSyntax.DescendantNodes().OfType<MethodDeclarationSyntax>()
                                          select _BuildCode(interfaceSyntax, methodSyntax);

            MethodInfosCode = string.Join(",", methods);

            IEnumerable<string> events =
                         from interfaceSyntax in _interfaces
                         from eventSyntax in interfaceSyntax.DescendantNodes().OfType<EventFieldDeclarationSyntax>()
                         select _BuildCode(interfaceSyntax, eventSyntax);

            EventInfosCode = string.Join(",", events);

            IEnumerable<string> propertys = from interfaceSyntax in _interfaces
                                            from propertySyntax in interfaceSyntax.DescendantNodes().OfType<PropertyDeclarationSyntax>()
                                            select _BuildCode(interfaceSyntax, propertySyntax);

            PropertyInfosCode = string.Join(",", propertys);


            IEnumerable<string> interfaces =
                             from interfaceSyntax in _interfaces

                             select _BuildCode(interfaceSyntax);

            InterfacesCode = string.Join(",", interfaces);
        }

        private string _BuildCode(InterfaceDeclarationSyntax interface_syntax)
        {
            var typeName = interface_syntax.GetNamePath();
            return
                $@"new System.Tuple<System.Type, System.Func<PinionCore.Remote.IProvider>>(typeof({typeName}),()=>new PinionCore.Remote.TProvider<{typeName}>())";
        }


        private string _BuildCode(InterfaceDeclarationSyntax interface_syntax, PropertyDeclarationSyntax property_syntax)
        {
            var typeName = interface_syntax.GetNamePath();
            var eventName = property_syntax.Identifier.ToString();
            return $@"typeof({typeName}).GetProperty(""{eventName}"")";
        }

        private string _BuildCode(InterfaceDeclarationSyntax interface_syntax, EventFieldDeclarationSyntax event_syntax)
        {

            var typeName = interface_syntax.GetNamePath();
            var eventName = event_syntax.Declaration.Variables[0].ToFullString();
            return $@"typeof({typeName}).GetEvent(""{eventName}"")";

        }


        private string _BuildCode(InterfaceDeclarationSyntax interface_syntax, MethodDeclarationSyntax method_syntax)
        {

            var typeName = interface_syntax.GetNamePath();

            IEnumerable<string> paramNames = method_syntax.ParameterList.Parameters.Count.GetSeries().Select(n => $"_{n + 1}");
            IEnumerable<string> paramTypes = method_syntax.ParameterList.Parameters.Select(p => p.Type.ToString());


            var typeAndParamTypes = string.Join(",", (new[] { typeName }).Concat(paramTypes));
            var instanceAndParamNames = string.Join(",", (new[] { "ins" }).Concat(paramNames));
            var paramNamesStr = string.Join(",", paramNames);
            return $"new PinionCore.Utility.Reflection.TypeMethodCatcher((System.Linq.Expressions.Expression<System.Action<{typeAndParamTypes}>>)(({instanceAndParamNames}) => ins.{method_syntax.Identifier}({paramNamesStr}))).Method";

        }
    }
}
