﻿# Regulus Remote
[![Maintainability](https://api.codeclimate.com/v1/badges/99cb5e1dc12cafbfe451/maintainability)](https://codeclimate.com/github/jiowchern/Regulus.Remote/maintainability)
[![Actions Status](https://github.com/jiowchern/Regulus.Remote/workflows/Build/badge.svg)](https://github.com/jiowchern/Regulus.Remote/actions)
[![Build status](https://ci.appveyor.com/api/projects/status/fv1owwit4utddawv/branch/release?svg=true)](https://ci.appveyor.com/project/jiowchern/regulus-remote/branch/release)
[![Coverage Status](https://coveralls.io/repos/github/jiowchern/Regulus.Remote/badge.svg?branch=master)](https://coveralls.io/github/jiowchern/Regulus.Remote?branch=master)
![commit last date](https://img.shields.io/github/last-commit/jiowchern/Regulus.Remote)  
<!-- [![Discord](https://img.shields.io/discord/101557008930451456.svg?label=&logo=discord&logoColor=ffffff&color=7389D8&labelColor=6A7EC2)](https://discord.gg/uDF8NTp) -->

<!-- [![GitHub release](https://img.shields.io/github/release/jiowchern/regulus.svg?style=flat-square)](https://github.com/jiowchern/Regulus/releases)![pre-release](https://img.shields.io/github/v/release/jiowchern/Regulus?include_prereleases) -->
<!-- [![Gitter](https://badges.gitter.im/JoinChat.svg)](https://gitter.im/Regulus-Library) -->


## Introduce
This is a server-client network connection framework developed based on .net standard 2.0 , available for unity game engine .

* [Feature](#Feature)
* [Communication](#Communication)
* [Getting Start](#Getting-Start)
* [Connection](#Connection)
* [Customisation](#Customisation)
* [Recommend](#Recommend)
* [Sample](#Sample)
## Feature
Server-side implementation interface, client-side use of objects from the server.
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
<!-- * Remote Method Invocation
* .Net Standard 2.0 base
* Compatible with Unity il2cpp
* Compatible with Unity WebGL
* Customizable connection
* Stand-alone mode  -->

## Latest Version
Download the latest ![Latest Version](https://img.shields.io/github/v/tag/jiowchern/Regulus.Remote) .


## Communication   
Instead of client communicating with server in packets, server send object to client through interface.  
Here are the steps to set up the communication.  
**A. Define the interface IGreeter.**  
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
		Regulus.Remote.Value<HelloReply> SayHello(HelloRequest request);
	}
}
```
**B. Implement the greeter class.**  
```csharp
namespace Server
{	
	class Greeter : IGreeter
	{				
		Regulus.Remote.Value<HelloReply> SayHello(HelloRequest request)
		{
			return new HelloReply() { Message = $"Hello {request.Name}." };
		}
	}
}
```
**C. Use binder to send Greeter to the client.**  
```csharp
namespace Server
{
	public class Entry	
	{
		readonly Greeter _Greeter;
		readonly Regulus.Remote.IBinder _Binder;
		readonly Regulus.Remote.ISoul _GreeterSoul;
		public Entry(Regulus.Remote.IBinder binder)
		{
			_Greeter = new Greeter();
			_Binder = binder;
			// bind to client.
			_GreeterSoul = binder.Bind<IBinder>(_Greeter);
		}
		public void Dispose()
		{
			// you can call Unbind to notify the client to cancel the greeter.  
			_Binder.Unbind(_GreeterSoul);
		}
	}
}
```
**D. Use an Agent to receive greeter from the server.**
```csharp
namespace Client
{
	class Entry
	{
		public Entry(Regulus.Remote.IAgent agent)
		{
			agent.QueryNotifier<Common.IGreeter>().Supply += _AddGreeter;
			agent.QueryNotifier<Common.IGreeter>().Unsupply += _RemoveGreeter;
		}
		async void  _AddGreeter(Common.IGreeter greeter)
		{						
			// todo: Having received the greeter from the server, 			 
			//       begin to implement the following code.
			var reply = await greeter.SayHello(new HelloRequest() {Name = "my"});
		}
		void _RemoveGreeter(Common.IGreeter greeter)
		{
			// todo: The server has canceled the greeter.
		}
	}
}
```
---
In this way, the server and the client can communicate through the interface and achieve object-oriented development as much as possible.  
<!-- In addition, bind and unbind are used to switch the objects of the server, so as to control the access rights of the client conveniently.  -->
The current communication capabilities of the interface are as follows...   
* [Method](document/communications-method.md)
* [Event](document/communications-event.md)
* [Property](document/communications-property.md)
* [Notifier](document/communications-notifier.md)
<!-- > Serialization supports the following types...  
> ```short, ushort, int, uint, bool, logn, ulong, float, decimal, double, char, byte, enum, string``` and array of the types. -->
          

## Getting Start
This is a server-client framework, so it requires at least three projects: **Protocol**, **Server** and **Client**.
### Dependency
* Visual Studio 2022  17.0.5 or above.
* .NET Sdk 6.0.101 or above.


### Protocol 
Create a message component to handle the communication requirements between client and server.  

**Create Project.**
```powershell
Sample/Protocol>dotnet new classlib 
```
Add references to **Protocol.csproj**.
```xml
<ItemGroup>
	<PackageReference Include="Regulus.Remote" Version="0.1.11.0" />
	<PackageReference Include="Regulus.Serialization" Version="0.1.11.0" />
	<PackageReference Include="Regulus.Remote.Tools.Protocol.Sources" Version="0.0.0.7">
		<PrivateAssets>all</PrivateAssets>
		<IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
	</PackageReference>	
</ItemGroup>
```
Add a sample file,**IFoo.cs**.
```csharp
namespace Protocol
{
	public interface IGreeter
	{
		Regulus.Remote.Value<HelloReply> SayHello(HelloRequest request);
	}
}
```
Create **ProtocolCreator**.
```csharp
namespace Protocol
{
    public static partial class ProtocolCreator
    {
        public static Regulus.Remote.IProtocol Create()
        {
            Regulus.Remote.IProtocol protocol = null;
            _Create(ref protocol);
            return protocol;
        }

        /*
			Create a partial method as follows.
        */
        [Regulus.Remote.Protocol.Creator]
        static partial void _Create(ref Regulus.Remote.IProtocol protocol);
    }
}
```

	
#### Server  
The following example sets up a server in console.  
```powershell
Sample/Server>dotnet new console 
```
Add references to **Server.csproj**.  
```xml
<ItemGroup>
	<PackageReference Include="Regulus.Remote.Server" Version="0.1.11.0" />
	<ProjectReference Include="..\Protocol\Protocol.csproj" />	
</ItemGroup>
```
The server needs an entry point for the startup environment.  
Create a entry class that inherits from the ```Regulus.Remote.IEntry```.
```csharp
namespace Server
{
	public class Entry : Regulus.Remote.IEntry
	{
		void IBinderProvider.AssignBinder(IBinder binder,object state)
		{
			// IBinder is what you get when your client completes the connection.
		}		
	}
}
```
Create service.
```csharp
namespace Server
{
	using System.Linq;
	static void Main(string[] args)
	{
		
		// Create protocol.
		var protocol = Messages.ProtocolCreator.Create();
		// your server entry.
		var entry = new Entry();

		// Create serializer. If you would like to customise your serialisation please see the Serialisation heading. 
		var serializer = new Regulus.Remote.Serializer(protocol.SerializeTypes);
		// Create service.
		var service = Regulus.Remote.Server.Provider.CreateService(entry, protocol,serializer );
		
		entry.Run();
	
		service.Dispose();
	}
}
```
#### Client
The following example sets up a client in console.  
```powershell
Sample/Client>dotnet new console 
```
Add references to **Client.csproj**.  
```xml
<ItemGroup>
	<PackageReference Include="Regulus.Remote.Client" Version="0.1.11.0" />
	<ProjectReference Include="..\Protocol\Protocol.csproj" />
</ItemGroup>
```
Create a ```Regulus.Remote.IAgent``` to handle the connection and receive objects from the server.
```csharp
var protocol = Protocol.ProtocolCreator.Create();
// Create serializer. If you would like to customise your serialisation please see the Serialisation heading. 
var serializer = new Regulus.Remote.Serializer(protocol.SerializeTypes);
var agent = Regulus.Remote.Client.Provider.CreateAgent(protocol,serializer);
// The agent uses single-thread continuations to process server requests and responses, so it needs to keep calling this method to stay operational. 
agent.Update(); 
```

Receive objects from the server side.
```csharp
var notifier = agent.QueryNotifier<Protocol.IGreeter>();
notifier.Supply += _AddGreeter; // The Supply is the Bind for the corresponding server.
notifier.Unsupply += _RemoveGreeter;// The Unsupply is the Unbind for the corresponding server.
```

## Connection
By default, Tcp connectivity is provided.  
**Listener**  
```csharp
var listener = Regulus.Remote.Server.CreateTcp(service);
listener.Bind(port);
listener.Close()
```
**Connecter**
```csharp
var connecter = Regulus.Remote.Client.CreateTcp(agent);
var online = connecter.Connect(ipaddress);
if(online != null)
	// connect success.
else
	// connect failed.
```
## Customisation
### Stream
If you want to customize. Simply provide ```IService``` and ```IAgent``` a **data stream**.  
Implement ```Regulus.Network.IStreamable```.  
```csharp
namespace Regulus.Network
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
**Server**
```csharp
class Server
{
	Regulus.Remote.Soul.IService _Service;
	// When listening on a connection.
	void Acctpt(Regulus.Network.IStreamable stream)
	{
		_Service.Join(stream);
	}
}
```
**Client**
```csharp
class Client
{
	void Start()
	{
		// custom connecter
		var connecter = CreateFromCustom();
		var agent = Regulus.Remote.Client.provider.CreateAgent(protocol);
		agent.Start(connecter);
		// begin your connect.
		connecter.Connect("127.0.0.1:21861");
	}
}
```
### Serialisation 

## Recommend
* [Regulus.Remote.CodeAnalysis](https://github.com/jiowchern/Regulus.Remote.CodeAnalysis) - Protocol syntax checker.
## Sample 
**[Regulus.Samples](https://github.com/jiowchern/Regulus.Samples)** ,This repository shows applications such as chat rooms.  




