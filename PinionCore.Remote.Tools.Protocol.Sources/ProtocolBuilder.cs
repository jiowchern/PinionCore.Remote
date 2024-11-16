using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace PinionCore.Remote.Tools.Protocol.Sources
{
    internal class ProtocolBuilder
    {
        public readonly string ProtocolName;
        public readonly SyntaxTree Tree;

        public ProtocolBuilder(Compilation compilation, EventProviderCodeBuilder event_provider_code_builder,
            InterfaceProviderCodeBuilder interface_provider_code_builder,
            MemberMapCodeBuilder membermap_code_builder, SerializableExtractor extractor)
        {
            var serCode = extractor.Code;
            var md5 = _BuildMd5(serCode + event_provider_code_builder.Code + interface_provider_code_builder.Code + membermap_code_builder.PropertyInfosCode + membermap_code_builder.EventInfosCode + membermap_code_builder.InterfacesCode + membermap_code_builder.MethodInfosCode);

            var protocolName = _BuildProtocolName(md5);
            var verCode = _BuildVersionCode(md5);
            var code = $@"
using System;  
using System.Collections.Generic;
using PinionCore.Remote;
public class {protocolName} : PinionCore.Remote.IProtocol
{{
  
    readonly PinionCore.Remote.InterfaceProvider _InterfaceProvider;
    readonly PinionCore.Remote.EventProvider _EventProvider;
    readonly PinionCore.Remote.MemberMap _MemberMap;
    
    readonly System.Reflection.Assembly _Base;
    readonly System.Type[] _SerializeTypes;
    public {protocolName}()
    {{
        _Base = System.Reflection.Assembly.Load(""{compilation.Assembly}"");
       
        _InterfaceProvider = new PinionCore.Remote.InterfaceProvider(new Dictionary<Type, Type> (){{ {interface_provider_code_builder.Code}}});
   
        _EventProvider = new PinionCore.Remote.EventProvider( new IEventProxyCreater[]{{ {event_provider_code_builder.Code} }});        
        _SerializeTypes = new System.Type[] {{{serCode}}};
        _MemberMap = new PinionCore.Remote.MemberMap(
            new System.Reflection.MethodInfo[] {{{membermap_code_builder.MethodInfosCode}}} ,
            new System.Reflection.EventInfo[]{{ {membermap_code_builder.EventInfosCode}}}, 
            new System.Reflection.PropertyInfo[] {{{membermap_code_builder.PropertyInfosCode}}}, 
            new System.Tuple<System.Type, System.Func<PinionCore.Remote.IProvider>>[] {{{membermap_code_builder.InterfacesCode}}});
    }}
    
    System.Reflection.Assembly PinionCore.Remote.IProtocol.Base =>_Base;
    System.Type[] PinionCore.Remote.IProtocol.SerializeTypes => _SerializeTypes;


    byte[] PinionCore.Remote.IProtocol.VersionCode {{ get {{ return new byte[]{{{verCode}}};}} }}
    PinionCore.Remote.InterfaceProvider PinionCore.Remote.IProtocol.GetInterfaceProvider()
    {{
        return _InterfaceProvider;
    }}

    PinionCore.Remote.EventProvider PinionCore.Remote.IProtocol.GetEventProvider()
    {{
        return _EventProvider;
    }}

   

    PinionCore.Remote.MemberMap PinionCore.Remote.IProtocol.GetMemberMap()
    {{
        return _MemberMap;
    }}  
    
}}
            
";
            ProtocolName = protocolName;
            Tree = SyntaxFactory.ParseSyntaxTree(code, null, $"ProtocolBuilder.{protocolName}.cs", Encoding.UTF8);
        }


        private string _BuildProtocolName(byte[] code)
        {
            return $"C{BitConverter.ToString(code).Replace("-", "")}";
        }
        private string _BuildVersionCode(byte[] code)
        {

            return string.Join(",", code.Select(val => val.ToString()).ToArray());
        }

        private byte[] _BuildMd5(string codes)
        {
            var md5 = MD5.Create();
            return md5.ComputeHash(Encoding.ASCII.GetBytes(codes));
        }


    }
}
