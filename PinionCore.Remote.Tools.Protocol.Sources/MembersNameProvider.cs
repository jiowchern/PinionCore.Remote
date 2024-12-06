namespace PinionCore.Remote.Tools.Protocol.Sources
{

    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    class MembersNameProvider
    {
        public string GetEventName(EventFieldDeclarationSyntax eventSyntax)
        {
            var namespaceName = GetNamespaceName(eventSyntax);
            var interfaceName = GetInterfaceName(eventSyntax);

            // EventField 可能宣告多個變數，通常只抓第一個即可
            var eventName = eventSyntax.Declaration.Variables.FirstOrDefault()?.Identifier.Text ?? "<unknownEvent>";

            return $"{namespaceName}.{interfaceName}.{eventName}";
        }

        public string GetEventName(EventDeclarationSyntax eventSyntax)
        {
            var eventName = eventSyntax.Identifier.Text;

            // 如果是顯式介面實作的事件 (例如 IMyInterface.MyEvent)
            // 透過 ExplicitInterfaceSpecifier 取得介面名稱
            if (eventSyntax.ExplicitInterfaceSpecifier != null)
            {
                var fullInterfaceName = GetQualifiedName(eventSyntax.ExplicitInterfaceSpecifier.Name);
                return $"{fullInterfaceName}.{eventName}";
            }

            // 否則以原本方法取得 namespace 和 interface
            var namespaceName = GetNamespaceName(eventSyntax);
            var interfaceName = GetInterfaceName(eventSyntax);
            return $"{namespaceName}.{interfaceName}.{eventName}";
        }

        public string GetPropertyName(PropertyDeclarationSyntax propertySyntax)
        {
            var namespaceName = GetNamespaceName(propertySyntax);
            var interfaceName = GetInterfaceName(propertySyntax);

            // PropertyDeclarationSyntax 的名稱在 Identifier
            var propertyName = propertySyntax.Identifier.Text;

            return $"{namespaceName}.{interfaceName}.{propertyName}";
        }

        public string GetMethodName(MethodDeclarationSyntax methodSyntax)
        {
            var namespaceName = GetNamespaceName(methodSyntax);
            var interfaceName = GetInterfaceName(methodSyntax);

            var methodName = methodSyntax.Identifier.Text;

            // 處理泛型型參: <T, U, ...>
            var genericPart = string.Empty;
            if (methodSyntax.TypeParameterList?.Parameters.Count > 0)
            {
                var typeParams = string.Join(", ", methodSyntax.TypeParameterList.Parameters.Select(p => p.Identifier.Text));
                genericPart = $"<{typeParams}>";
            }

            // 處理參數清單: (int arg1, string arg2, ...)
            var argsPart = string.Empty;
            if (methodSyntax.ParameterList != null && methodSyntax.ParameterList.Parameters.Count > 0)
            {
                var args = string.Join(", ", methodSyntax.ParameterList.Parameters.Select(p => p.Type?.ToString() ?? "object"));
                argsPart = $"({args})";
            }
            else
            {
                argsPart = "()";
            }

            return $"{namespaceName}.{interfaceName}.{methodName}{genericPart}{argsPart}";
        }

        private string GetInterfaceName(SyntaxNode memberSyntax)
        {
            // 自下而上尋找最近的 InterfaceDeclarationSyntax
            var interfaceDecl = memberSyntax.Ancestors().OfType<InterfaceDeclarationSyntax>().FirstOrDefault();
            if (interfaceDecl != null)
            {
                return interfaceDecl.Identifier.Text;
            }
            return "<unknownInterface>";
        }

        private string GetNamespaceName(SyntaxNode memberSyntax)
        {
            // 自下而上尋找 NamespaceDeclarationSyntax 或 FileScopedNamespaceDeclarationSyntax
            var namespaceDecl = memberSyntax.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
            if (namespaceDecl != null)
            {
                return namespaceDecl.Name.ToString();
            }

            /*var fileScopedNamespaceDecl = memberSyntax.Ancestors().OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault();
            if (fileScopedNamespaceDecl != null)
            {
                return fileScopedNamespaceDecl.Name.ToString();
            }*/

            // 如果沒有命名空間(可能是 Global Namespace)
            return "<global>";
        }

        private string GetQualifiedName(NameSyntax nameSyntax, bool isTopLevel = true)
        {
            if (nameSyntax is QualifiedNameSyntax qns)
            {
                // 遞迴取得左側命名 (可能是命名空間或更深的合併名稱)
                var left = GetQualifiedName(qns.Left, false);
                // 返回 left.right 形式的字串
                return $"{left}.{qns.Right.Identifier.Text}";
            }
            else if (nameSyntax is IdentifierNameSyntax ins)
            {
                // 若是最上層直接就是一個 Identifier 沒有命名空間, 則加上 <global>
                if (isTopLevel)
                {
                    return $"<global>.{ins.Identifier.Text}";
                }
                else
                {
                    // 非最上層 (例如在 QualifiedNameSyntax 遞迴中) 表示此 Identifier
                    // 是命名空間或類型名稱的一部分，不需要加 <global>
                    return ins.Identifier.Text;
                }
            }

            return string.Empty;
        }

    }
}
