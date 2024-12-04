using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
namespace PinionCore.Remote.Tools.Protocol.Sources
{
    class MemberIdProvider
    {

        public readonly InterfaceDeclarationSyntax[] Souls;
        public readonly MemberDeclarationSyntax[] Members;        
        

        public MemberIdProvider(IEnumerable<InterfaceDeclarationSyntax> souls)
        {
            var members = from interfaceSyntax in souls
                          from methodSyntax in interfaceSyntax.DescendantNodes().OfType<MemberDeclarationSyntax>()
                          select methodSyntax;

            Members = members.ToArray();
            Souls = souls.ToArray();
        }

        
        public int GetId(MemberDeclarationSyntax member)
        {
            int id = 0;
            foreach(var m in Members)
            {
                ++id;
                if (m.IsEquivalentTo(member))
                {
                    return id;
                }
                else if (!IsImplemented(m,member))
                {
                    continue;
                }

                return id;
            }

            throw new System.Exception($"Member not found {member}");
        }

        bool IsImplemented(MemberDeclarationSyntax interfaceMember, MemberDeclarationSyntax classMember)
        {
            // 檢查類型是否相容
            if (!AreMemberTypesCompatible(interfaceMember, classMember))
            {
                return false; // 類型不相容，直接返回 false
            }

            // 檢查 classMember 是否有明確的介面實作
            ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier = GetExplicitInterfaceSpecifier(classMember);

            if (explicitInterfaceSpecifier == null)
            {
                return false; // classMember 沒有明確介面實作
            }

            // 提取介面名稱
            var interfaceName = explicitInterfaceSpecifier.Name.ToString();

            // 確認名稱是否匹配
            string interfaceMemberName = GetMemberName(interfaceMember);
            string classMemberName = GetMemberName(classMember);

            if (interfaceMemberName != classMemberName)
                return false;

            if (!(interfaceMember.Parent is InterfaceDeclarationSyntax interfaceDeclaration))
                return false;

            // 提取介面完整名稱，包括命名空間
            var fullInterfaceName = GetFullInterfaceName(interfaceDeclaration);

            if (fullInterfaceName != interfaceName)
                return false;

            return true;
        }

        string GetFullInterfaceName(InterfaceDeclarationSyntax interfaceDeclaration)
        {
            // 遍歷語法樹，向上找到命名空間宣告
            var namespaceDeclaration = interfaceDeclaration.Parent as NamespaceDeclarationSyntax;

            if (namespaceDeclaration == null)
            {
                return interfaceDeclaration.Identifier.Text; // 沒有命名空間，返回介面名稱
            }

            // 組合命名空間與介面名稱
            return $"{namespaceDeclaration.Name}.{interfaceDeclaration.Identifier.Text}";
        }

        // 檢查成員類型是否相容
        bool AreMemberTypesCompatible(MemberDeclarationSyntax interfaceMember, MemberDeclarationSyntax classMember)
        {
            // 介面成員可能是 EventFieldDeclarationSyntax，類別成員可能是 EventDeclarationSyntax
            if (interfaceMember is EventFieldDeclarationSyntax && classMember is EventDeclarationSyntax)
            {
                return true;
            }            

            // 其他成員類型需要類型完全一致
            return interfaceMember.GetType() == classMember.GetType();
        }

        // 取得成員的 ExplicitInterfaceSpecifier
        ExplicitInterfaceSpecifierSyntax GetExplicitInterfaceSpecifier(MemberDeclarationSyntax member)
        {
            if (member is MethodDeclarationSyntax method)
            {
                return method.ExplicitInterfaceSpecifier;
            }
            else if (member is PropertyDeclarationSyntax property)
            {
                return property.ExplicitInterfaceSpecifier;
            }
            else if (member is EventDeclarationSyntax evt)
            {
                return evt.ExplicitInterfaceSpecifier;
            }            
            else
            {
                return null; // 不支援的成員類型或沒有明確實作
            }
        }

        // 輔助函數：提取成員名稱
        string GetMemberName(MemberDeclarationSyntax member)
        {
            if (member is MethodDeclarationSyntax method)
            {
                return method.Identifier.Text;
            }
            else if (member is PropertyDeclarationSyntax property)
            {
                return property.Identifier.Text;
            }
            else if (member is EventDeclarationSyntax evt)
            {
                return evt.Identifier.Text;
            }
            else if (member is EventFieldDeclarationSyntax eventField)
            {
                // 介面中的事件欄位宣告，可能有多個變數，通常只有一個
                var variable = eventField.Declaration.Variables.FirstOrDefault();
                return variable?.Identifier.Text;
            }
            else
            {
                return null; // 不支援的類型
            }
        }



    }

}
