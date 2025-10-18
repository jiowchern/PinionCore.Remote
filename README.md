﻿


# PinionCore Remote
[![Maintainability](https://api.codeclimate.com/v1/badges/89c3a646f9daff42a38e/maintainability)](https://codeclimate.com/github/jiowchern/PinionCore.Remote/maintainability)
[![Build](https://github.com/jiowchern/PinionCore.Remote/actions/workflows/dotnet-desktop.yml/badge.svg?branch=master)](https://github.com/jiowchern/PinionCore.Remote/actions/workflows/dotnet-desktop.yml)
[![Coverage Status](https://coveralls.io/repos/github/jiowchern/PinionCore.Remote/badge.svg?branch=master)](https://coveralls.io/github/jiowchern/PinionCore.Remote?branch=master)
![commit last date](https://img.shields.io/github/last-commit/jiowchern/PinionCore.Remote) 
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/jiowchern/PinionCore.Remote)  
<!-- [![Discord](https://img.shields.io/discord/101557008930451456.svg?label=&logo=discord&logoColor=ffffff&color=7389D8&labelColor=6A7EC2)](https://discord.gg/uDF8NTp) -->
<!-- [![Build status](https://ci.appveyor.com/api/projects/status/fv1owwit4utddawv/branch/release?svg=true)](https://ci.appveyor.com/project/jiowchern/regulus-remote/branch/release) -->
<!-- [![GitHub release](https://img.shields.io/github/release/jiowchern/regulus.svg?style=flat-square)](https://github.com/jiowchern/PinionCore/releases)![pre-release](https://img.shields.io/github/v/release/jiowchern/PinionCore?include_prereleases) -->
<!-- [![Gitter](https://badges.gitter.im/JoinChat.svg)](https://gitter.im/PinionCore-Library) -->

<!-- ![NuGet Downloads](https://img.shields.io/nuget/dt/PinionCore.Remote) -->
[中文說明（繁體）](README-tc.md)

## Introduction
PinionCore Remote is a powerful and flexible server-client communication framework developed in C#. Designed to work seamlessly with the Unity game engine and any other .NET Standard 2.0 compliant environments, it simplifies network communication by enabling servers and clients to interact through interfaces. This object-oriented approach reduces the maintenance cost of protocols and enhances code readability and maintainability.  

Key features of PinionCore Remote include support for IL2CPP and AOT, making it compatible with various platforms, including Unity WebGL. It provides default TCP connection and serialization mechanisms but also allows for customization to suit specific project needs. The framework supports methods, events, properties, and notifiers, giving developers comprehensive tools to build robust networked applications.  

With its stand-alone mode, developers can simulate server-client interactions without a network connection, facilitating development and debugging. PinionCore Remote aims to streamline network communication in game development and other applications, enabling developers to focus more on implementing business logic rather than dealing with the complexities of network protocols.  

If you want to know the details of the system architecture you can refer to Ask --> [DeepWiki](https://deepwiki.com/jiowchern/PinionCore.Remote) Or [OpenDeepWiki](https://opendeep.wiki/jiowchern/PinionCore.Remote) 


<!-- * Remote Method Invocation
* .Net Standard 2.0 base
* Compatible with Unity il2cpp
* Compatible with Unity WebGL
* Customizable connection
* Stand-alone mode  -->

## Feature
Server and client transfer through the interface, reducing the maintenance cost of the protocol.
<!-- 
@startuml
package Protocol <<Rectangle>>{
    interface IGreeter {
        +SayHello()
    }
}


package Server <<Rectangle>> {
    class Greeter {
        +SayHello()
    }
}

package Client <<Rectangle>>{
    class SomeClass{
        {field} IGreeter greeter 
    }
}

IGreeter --* SomeClass::greeter 
IGreeter <|.. Greeter  

note left of Greeter  
    Implement IGreeter 
end note

note right of SomeClass::greeter 
    Use object from server.
end note

@enduml
-->
![plantUML](http://www.plantuml.com/plantuml/svg/ZP31JiCm38RlUGeVGMXzWAcg9kq0ko4g7Y1aVql1IIR7GqAZxqvRLGdiD5zgsVw_ViekgHKzUpOdwpvj3tgMgD55fhf-WLCRUaRJN0nDDGI5TDQ13ey2A8IcnLeFhVr-0dEykrzcencDoTWMyWNv3rt3ZcrAT1EmyFOy8EYrPC6rqMC_TuLtwGRmSIpk_VejzBpQR9g2s6xpPJweVwegEvCn8Ig8qId5himNyi6V67wspMc3SAGviWPbwD_dvDK_Yzrh0iMt3pYbJgAdj3ndzOUpczgpvry0)
## Supports
* Support **IL2CPP & AOT**.  
* Compatible with **.Net Standard2.0** or above development environment.
* **Tcp** connection is provided by default, and any connection can be customized according to your needs.
* **Serialization** is provided by default, and can be customized.
* Support **Unity3D WebGL**, provide server-side Websocket, client-side need to implement their own.
 
## Usage
1. Definition Interface ```IGreeter``` .

```csharp
namespace Protocol
{
	public struct HelloRequest
	{
		public string Name;
	}
	public struct HelloReply
	{
		public string Message;
	}
	public interface IGreeter
	{
		PinionCore.Remote.Value<HelloReply> SayHello(HelloRequest request);
	}
}
```
2. Server implements ```IGreeter```.
```csharp
namespace Server
{	
	class Greeter : IGreeter
	{				
		PinionCore.Remote.Value<HelloReply> SayHello(HelloRequest request)
		{
			return new HelloReply() { Message = $"Hello {request.Name}." };
		}
	}
}
```

3. Use ```IBinder.Bind``` to send the ```IGreeter``` to the client.
```csharp
namespace Server
{
	public class Entry	
	{
		readonly Greeter _Greeter;
		readonly PinionCore.Remote.IBinder _Binder;
		readonly PinionCore.Remote.ISoul _GreeterSoul;
		public Entry(PinionCore.Remote.IBinder binder)
		{
			_Greeter = new Greeter();
			_Binder = binder;
			// bind to client.
			_GreeterSoul = binder.Bind<IGreeter>(_Greeter);
		}
		public void Dispose()
		{			
			_Binder.Unbind(_GreeterSoul);
		}
	}
}
```

4. Client uses ```IAgent.QueryNotifier``` to obtain ```IGreeter```.
```csharp
namespace Client
{
	class Entry
	{
		public Entry(PinionCore.Remote.IAgent agent)
		{
			agent.QueryNotifier<IGreeter>().Supply += _AddGreeter;
			agent.QueryNotifier<IGreeter>().Unsupply += _RemoveGreeter;
		}
		async void  _AddGreeter(IGreeter greeter)
		{						
			// Having received the greeter from the server, 			 
			// begin to implement the following code.
			var reply = await greeter.SayHello(new HelloRequest() {Name = "my"});
		}
		void _RemoveGreeter(IGreeter greeter)
		{
			// todo: The server has canceled the greeter.
		}
	}
}
```
---
After completing the above steps, the server and client can communicate through the interface to achieve object-oriented development as much as possible.
#### Specification
**Interface**  
In addition to the above example ``IGreeter.SayHello``, there are a total of four ways to ...

<!-- In addition, bind and unbind are used to switch the objects of the server, so as to control the access rights of the client conveniently.  -->
* [```Method```](document/communications-method.md) <-- ```IGreeter.SayHello``` 
* [```Event```](document/communications-event.md)
* [```Property```](document/communications-property.md)
* [```Notifier```](document/communications-notifier.md)

**Streamable Method**
If an interface declares a method with the signature `PinionCore.Remote.IAwaitableSource<int> MethodName(byte[] buffer, int offset, int count)`, the source generator will wire it as a streamable call. Only the slice described by `offset` and `count` is sent to the server, and the server response returns both the processed byte count and the updated data so the client buffer is filled back starting at `offset`. Use this pattern when you need bidirectional streaming without shipping the entire byte array over the network.

**Serialization**  
For the types that can be serialized, see [```PinionCore.Serialization```](PinionCore.Serialization/README.md) instructions.
<!-- > Serialization supports the following types...  
> ```short, ushort, int, uint, bool, logn, ulong, float, decimal, double, char, byte, enum, string``` and array of the types. -->
          
---
## Getting Started
This is a server-client framework, so you need to create three projects : ```Protocol```, ```Server``` and ```Client```.

#### Requirements
* Visual Studio 2022  17.0.5 above.
* .NET Sdk 5 above. 

#### Protocol Project
Create common interface project ```Protocol.csproj```.
```powershell
Sample/Protocol>dotnet new classlib 
```
<!-- Add references to **Protocol.csproj**. -->
1. Add References
```xml
<ItemGroup>
	<PackageReference Include="PinionCore.Remote" Version="0.1.13.15" />
	<PackageReference Include="PinionCore.Serialization" Version="0.1.13.12" />
	<PackageReference Include="PinionCore.Remote.Tools.Protocol.Sources" Version="0.0.1.25">
		<PrivateAssets>all</PrivateAssets>
		<IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
	</PackageReference>	
</ItemGroup>
```
2. Add interface, ```IGreeter.cs```
```csharp
namespace Protocol
{
	public interface IGreeter
	{
		PinionCore.Remote.Value<string> SayHello(string request);
	}
}
```
3. Add ```ProtocolCreator.cs```.
```csharp
namespace Protocol
{
    public static partial class ProtocolCreator
    {
        public static PinionCore.Remote.IProtocol Create()
        {
            PinionCore.Remote.IProtocol protocol = null;
            _Create(ref protocol);
            return protocol;
        }

        /*
			Create a partial method as follows.
        */
        [PinionCore.Remote.Protocol.Creator]
        static partial void _Create(ref PinionCore.Remote.IProtocol protocol);
    }
}
```  
This step is to generate the generator for the ``IProtocol``, which is an important component of the framework and is needed for communication between the server and the client.  
**_Note_**  
>> As shown in the code above, Add ```PinionCore.Remote.Protocol``` attribute to the method you want to get ```IProtocol```, the method specification must be ```static partial void Method(ref PinionCore.Remote.IProtocol)```, otherwise it will not pass compilation.

---
	
#### Server Project
Create the server. ```Server.csproj```
```powershell
Sample/Server>dotnet new console 
```
1. Add References
```xml
<ItemGroup>
	<PackageReference Include="PinionCore.Remote.Server" Version="0.1.13.13" />
	<ProjectReference Include="..\Protocol\Protocol.csproj" />	
</ItemGroup>
```
2. Instantiate ```IGreeter```
```csharp
namespace Server
{
	public class Greeter : Protocol.IGreeter
	{
		PinionCore.Remote.Value<string> SayHello(string request)
		{
			// Return the received message
			return $"echo:{request}";
		}
	}
}
```
3. The server needs an entry point to start the environment , creating an entry point that inherits from ``PinionCore.Remote.IEntry``. ```Entry.cs```
```csharp
namespace Server
{
	public class Entry : PinionCore.Remote.IEntry
	{		
		void IBinderProvider.RegisterClientBinder(IBinder binder)
		{					
			binder.Binder<IGreeter>(new Greeter());
		}		
		void IBinderProvider.UnregisterClientBinder(IBinder binder)
		{
			// when client disconnect.
		}

		void IEntry.Update()
		{
			// Update
		}
	}
}
```
4. Create Tcp service
```csharp

namespace Server
{	
	static void Main(string[] args)
	{		
		// Get IProtocol with ProtocolCreator
		var protocol = Protocol.ProtocolCreator.Create();
		
		// Create Service
		var entry = new Entry();		
		
		var set = PinionCore.Remote.Server.Provider.CreateTcpService(entry, protocol);
		int yourPort = 0;
		set.Listener.Bind(yourPort);
				
		//  Close service
		set.Listener.Close();
		set.Service.Dispose();
	}
}


```
---
#### Client Project
Create Client. ```Client.csproj```.  
```powershell
Sample/Client>dotnet new console 
```
1. Add References
```xml
<ItemGroup>
	<PackageReference Include="PinionCore.Remote.Client" Version="0.1.13.12" />
	<ProjectReference Include="..\Protocol\Protocol.csproj" />
</ItemGroup>
```
2. Create Tcp client
```csharp
namespace Client
{	
	static async Task Main(string[] args)
	{		
		// Get IProtocol with ProtocolCreator
		var protocol = Protocol.ProtocolCreator.Create();
				
		var set = PinionCore.Remote.Client.Provider.CreateTcpAgent(protocol);
		
		bool stop = false;
		var task = System.Threading.Tasks.Task.Run(() => 
		{
			while (!stop)
			{
				set.Agent.HandleMessages();
				set.Agent.HandlePackets();
			}
                
		});
		// Start Connecting
		EndPoint yourEndPoint = null;
		var peer = await set.Connector.Connect(yourEndPoint );

		set.Agent.Enable(peer);

		// SupplyEvent ,Receive add IGreeter.
		set.Agent.QueryNotifier<Protocol.IGreeter>().Supply += greeter => 
		{			
			greeter.SayHello("hello");
		};

		// SupplyEvent ,Receive remove IGreeter.
		set.Agent.QueryNotifier<Protocol.IGreeter>().Unsupply += greeter => 
		{
			
		};

		// Close
		stop = true;
		task.Wait();
		set.Connector.Disconnect();
		set.Agent.Disable();

	}
}
```
---
## Standalone mode
In order to facilitate development and debugging, a standalone mode is provided to run the system without a connection.
```powershell
Sample/Standalone>dotnet new console 
```
1. Add References
```xml
<ItemGroup>
	<PackageReference Include="PinionCore.Remote.Standalone" Version="0.1.13.14" />
	<ProjectReference Include="..\Protocol\Protocol.csproj" />
	<ProjectReference Include="..\Server\Server.csproj" />
</ItemGroup>
```
2.  Create standalone service
```csharp
namespace Standalone
{	
	static void Main(string[] args)
	{		
		// Get IProtocol with ProtocolCreator
		var protocol = Protocol.ProtocolCreator.Create();
		
		// Create service
		var entry = new Entry();
		var service = PinionCore.Remote.Standalone.Provider.CreateService(entry , protocol);
		var agent = service.Create();
		
		bool stop = false;
		var task = System.Threading.Tasks.Task.Run(() => 
		{
			while (!stop)
			{
				agent.HandleMessages();
				agent.HandlePackets();

			}
                
		});		
		
		agent.QueryNotifier<Protocol.IGreeter>().Supply += greeter => 
		{
		
			greeter.SayHello("hello");
		};
		
		agent.QueryNotifier<Protocol.IGreeter>().Unsupply += greeter => 
		{
			
		};

		// Close
		stop = true;
		task.Wait();
		
		agent.Dispose();
		service.Dispose();		

	}
}
```
---
## Custom Connection
If you want to customize the connection system you can do so in the following way.
#### Client
Create a connection use ```CreateAgent``` and implement the interface ```IStreamable```.
```csharp
var protocol = Protocol.ProtocolCreator.Create();
IStreamable stream = null ;// todo: Implementation Interface IStreamable
var service = PinionCore.Remote.Client.CreateAgent(protocol , stream) ;
```
implement ```IStreamable```.
```csharp
using PinionCore.Remote;
namespace PinionCore.Network
{
    public interface IStreamable
    {
        /// <summary>
        ///     Receive data streams.
        /// </summary>
        /// <param name="buffer">Stream instance.</param>
        /// <param name="offset">Start receiving position.</param>
        /// <param name="count">Count of byte received.</param>
        /// <returns>Actual count of byte received.</returns>
        IWaitableValue<int> Receive(byte[] buffer, int offset, int count);
        /// <summary>
        ///     Send data streams.
        /// </summary>
        /// <param name="buffer">Stream instance.</param>
        /// <param name="offset">Start send position.</param>
        /// <param name="count">Count of byte send.</param>
        /// <returns>Actual count of byte send.</returns>
        IWaitableValue<int> Send(byte[] buffer, int offset, int count);
    }
}
```

#### Server

Create a service use ```CreateService``` and implement the interface ```IListenable```.
```csharp
var protocol = Protocol.ProtocolCreator.Create();
var entry = new Entry();
IListenable listener = null; // todo: Implementation Interface IListenable
var service = PinionCore.Remote.Server.CreateService(entry , protocol , listener) ;
```
implement ```IListenable```.
```csharp
namespace PinionCore.Remote.Soul
{
    public interface IListenable
    {
		// When connected
        event System.Action<Network.IStreamable> StreamableEnterEvent;
		// When disconnected
        event System.Action<Network.IStreamable> StreamableLeaveEvent;
    }
}
```
---
## Custom Serialization
implement ```ISerializable```.
```csharp
namespace PinionCore.Remote
{
    public interface ISerializable
    {
        PinionCore.Memorys.Buffer Serialize(System.Type type, object instance);
        object Deserialize(System.Type type, PinionCore.Memorys.Buffer buffer);
    }
}
```
and bring it to the server ```CreateTcpService```.
```csharp
var protocol = Protocol.ProtocolCreator.Create();
var entry = new Entry();
ISerializable yourSerializer = null; 
var service = PinionCore.Remote.Server.CreateTcpService(entry , protocol , yourSerializer) ;
```

and bring it to the client ```CreateTcpAgent```.
```csharp
var protocol = Protocol.ProtocolCreator.Create();
ISerializable yourSerializer = null ;
var service = PinionCore.Remote.Client.CreateTcpAgent(protocol , yourSerializer) ;
```  

If need to know what types need to be serialized can refer ```PinionCore.Remote.IProtocol.SerializeTypes```.  
```csharp
namespace PinionCore.Remote
{
	public interface IProtocol
	{
		// What types need to be serialized.
		System.Type[] SerializeTypes { get; }
				
		System.Reflection.Assembly Base { get; }
		EventProvider GetEventProvider();
		InterfaceProvider GetInterfaceProvider();
		MemberMap GetMemberMap();
		byte[] VersionCode { get; }
	}
}
```


