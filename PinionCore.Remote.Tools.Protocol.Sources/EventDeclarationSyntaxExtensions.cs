using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace PinionCore.Remote.Tools.Protocol.Sources
{

    public static class EventDeclarationSyntaxExtensions
    {
        public static ClassDeclarationSyntax CreatePinionCoreRemoteIEventProxyCreator(this EventDeclarationSyntax eds)
        {

            var een = eds.ExplicitInterfaceSpecifier.Name.ToString().Replace('.', '_');

            var fieldName = eds.Identifier.ValueText;
            var typeName = eds.ExplicitInterfaceSpecifier.Name.ToString();
            var className = $"C{een}_{eds.Identifier}";
            QualifiedNameSyntax baseName = QualifiedName(
                            QualifiedName(
                                IdentifierName("PinionCore"),
                                IdentifierName("Remote")
                            ),
                            IdentifierName("IEventProxyCreator")
                        );

            ParameterListSyntax paramList = SyntaxFactory.ParameterList();


            var paramExpression = "";
            var typesExpression = "";
            if (eds.Type is QualifiedNameSyntax qn)
            {

                TypeArgumentListSyntax typeList = qn.Right.DescendantNodes().OfType<TypeArgumentListSyntax>().FirstOrDefault() ?? SyntaxFactory.TypeArgumentList();
                var paramCount = typeList.Arguments.Count;

                var names = new string[paramCount];
                for (var i = 0; i < paramCount; i++)
                {
                    var name = $"_{i + 1}";
                    names[i] = name;
                    paramList = paramList.AddParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier(name)));
                }
                paramExpression = string.Join(",", names);
                if (paramCount > 0)
                    typesExpression = typeList.ToString();
            }



            var source = $@"
            
            class {className} : PinionCore.Remote.IEventProxyCreator
                {{
            
                    System.Type _Type;
                    string _Name;
                    
                    public {className}()
                    {{
                        _Name = ""{fieldName}"";
                        _Type = typeof({typeName});                   
                    
                    }}
                    System.Delegate PinionCore.Remote.IEventProxyCreator.Create(long soul_id,int event_id,long handler_id, PinionCore.Remote.InvokeEventCallabck invoke_Event)
                    {{                
                        var closure = new PinionCore.Remote.GenericEventClosure(soul_id , event_id ,handler_id, invoke_Event);                
                        return new System.Action{typesExpression}(({paramExpression}) => closure.Run(new object[]{{{paramExpression}}}));
                    }}
                
            
                    System.Type PinionCore.Remote.IEventProxyCreator.GetType()
                    {{
                        return _Type;
                    }}            
            
                    string PinionCore.Remote.IEventProxyCreator.GetName()
                    {{
                        return _Name;
                    }}            
                }}
                        ";

            Microsoft.CodeAnalysis.SyntaxTree tree = CSharpSyntaxTree.ParseText(source);

            return tree.GetRoot().DescendantNodesAndSelf().OfType<ClassDeclarationSyntax>().Single();
        }
    }
}
