# PinionCore Remote
[![Maintainability](https://api.codeclimate.com/v1/badges/89c3a646f9daff42a38e/maintainability)](https://codeclimate.com/github/jiowchern/PinionCore.Remote/maintainability)
[![Build](https://github.com/jiowchern/PinionCore.Remote/actions/workflows/dotnet-desktop.yml/badge.svg?branch=master)](https://github.com/jiowchern/PinionCore.Remote/actions/workflows/dotnet-desktop.yml)
[![Coverage Status](https://coveralls.io/repos/github/jiowchern/PinionCore.Remote/badge.svg?branch=master)](https://coveralls.io/github/jiowchern/PinionCore.Remote?branch=master)
![commit last date](https://img.shields.io/github/last-commit/jiowchern/PinionCore.Remote)
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/jiowchern/PinionCore.Remote)  
[Ask OpenDeepWiki](https://opendeep.wiki/jiowchern/PinionCore.Remote/introduction?branch=master)  
[中文说明](README.TC.md)  


## Introduction

PinionCore Remote is an object-oriented remote communication framework developed in C#.
You can define communication protocols using "interfaces" - the server implements these interfaces, and the client invokes them as if calling local objects, while the actual data is transmitted through TCP / WebSocket / Standalone simulation channels.

- Supports .NET Standard 2.1 (.NET 6/7/8, Unity 2021+)
- Supports IL2CPP and AOT (requires pre-registration of serialization types)
- Built-in TCP, WebSocket, and Standalone single-machine mode
- Automatically generates `IProtocol` implementation through Source Generator, reducing maintenance costs

## Core Features

### 1. Interface-Oriented Communication

Only need to define interfaces, no need to manually write serialization and protocol parsing:

```csharp
public interface IGreeter
{
    PinionCore.Remote.Value<HelloReply> SayHello(HelloRequest request);
}
```
Server implements the interface:
```csharp
class Greeter : IGreeter
{
    PinionCore.Remote.Value<HelloReply> IGreeter.SayHello(HelloRequest request)
    {
        return new HelloReply { Message = $"Hello {request.Name}." };
    }
}
```
Client gets the remote proxy through QueryNotifier<IGreeter>(), calls SayHello directly, and the returned Value<T> can be awaited.
```csharp
agent.QueryNotifier<IGreeter>().Supply += greeter =>
{
    var request = new HelloRequest { Name = "you" };
    greeter.SayHello(request).OnValue += reply =>
    {
        Console.WriteLine($"Receive message: {reply.Message}");
    };
};
```

### 2. Controllable Lifecycle (Entry / Session / Soul)

The server entry implements PinionCore.Remote.IEntry, receives ISessionBinder when a connection is established, and you decide when to bind/unbind interfaces:
```csharp
public class Entry : PinionCore.Remote.IEntry
{
    private readonly Greeter _greeter = new Greeter();

    void PinionCore.Remote.ISessionObserver.OnSessionOpened(PinionCore.Remote.ISessionBinder binder)
    {
        // Client connected successfully, bind _greeter
        var soul = binder.Bind<IGreeter>(_greeter);


        // To unbind, call this line
        binder.Unbind(soul);
    }

    void PinionCore.Remote.ISessionObserver.OnSessionClosed(PinionCore.Remote.ISessionBinder binder)
    {
        // Cleanup when client disconnects
    }

    void PinionCore.Remote.IEntry.Update()
    {
        // Per-loop update (can be empty, depending on requirements)
    }
}
```
The server uses Host to create services (Host inherits from Soul.Service, internally manages all connections and Sessions through SessionEngine): `new PinionCore.Remote.Server.Host(entry, protocol)`.


### 3. Value / Property / Notifier Support

PinionCore.Remote is centered around "interfaces" and provides three common member types to describe remote behavior and state:

- **Value\<T>**: Describes "one-time asynchronous calls"
  - Used for method return values (similar to the Task\<T> concept)
  - Suitable for request/response flows, such as: login, get settings, submit commands, etc.
  - The caller only needs to wait for the result, no need to maintain long-term state

- **Property**: Describes "stable remote state"
  - Properties on the interface are implemented by the server, and the client reads them through proxies
  - Suitable for representing relatively stable information, such as: player name, room title, server version, etc.
  - Combined with events or Notifier, can notify the client to update UI when state changes

- **Notifier\<T>: Dynamic collection supporting nested interfaces and object tree synchronization**
  `INotifier<T>` is used to represent "a set of dynamically existing remote objects".
  Notably, `T` can not only be a primitive type, but also **an interface itself**, allowing natural description of **nested object structures (object trees)** and synchronizing the lifecycle of this tree between server and client.

  Typical scenarios include hierarchical structures like Lobby / Room / Player:

  ```csharp
  public interface IChatEntry
  {
      // Dynamic list of all current rooms
      INotifier<IRoom> Rooms { get; }
  }

  public interface IRoom
  {
      Property<string> Name { get; }
      // Dynamic list of players in the room
      INotifier<IPlayer> Players { get; }
  }

  public interface IPlayer
  {
      Property<string> Nickname { get; }
  }
  ```

  - The server maintains the actual room and player objects, corresponding to `INotifier<IRoom>`, `INotifier<IPlayer>`
    - When a room is created, the server "supplies" an `IRoom` instance to `Rooms`
    - When a room is deleted, that `IRoom` is removed from `Rooms`
    - When players enter/exit a room, the corresponding `IPlayer` is supplied/removed from that `IRoom.Players`
  - The client only needs to get `INotifier<IRoom>` through the interface to:
    - Automatically receive "room added/removed" notifications
    - For each room, continue subscribing to `room.Players` and automatically receive "player entered/left" notifications
    - The obtained `IRoom` / `IPlayer` are all remote proxies, just call their interface members directly

  Through this design, Notifier is not just a "collection of events", but:

  - Used to describe "dynamic collections" and "changing object trees"
  - Supports nested interfaces: `INotifier<IRoom>` → `IRoom` contains `INotifier<IPlayer>` → even deeper sub-modules
  - The client does not need to manage any ids or lookup logic, just access by interface hierarchy, and automatically track the creation and destruction of server-side objects

  **Summary**:
  - `Value<T>`: One-time call result
  - `Property`: Stable state value
  - `Notifier<T>`: Synchronizes "collections of objects that can grow/shrink", and supports nested object trees with interfaces as nodes, which is the core capability of PinionCore.Remote for expressing complex remote structures.

#### Notifier Supply / Removal Process Overview

Using `IChatEntry.Rooms` as an example, the operation of Notifier can be understood through the following process:

1. After the server starts, create `IRoom` implementation objects and supply them through `INotifier<IRoom>`:
   - Call `Rooms.Supply(roomImpl)` when a room exists
   - Call `Rooms.Unsupply(roomImpl)` when a room closes
2. The communication layer forwards these supply/removal events to each connected client.
3. The client gets the corresponding `INotifier<IRoom>` proxy through `agent.QueryNotifier<IRoom>()` and subscribes:

   ```csharp
   agent.QueryNotifier<IRoom>().Supply += room =>
   {
       // The room here is already a remote proxy and can be used directly
       room.Players.Supply += player =>
       {
           // Handle player join event
       };
   };
   ```

4. When the server Unsupplies an object, the client receives the corresponding `Unsupply` event and automatically releases that proxy.

Through this mechanism, the server only needs to manage the lifecycle of real objects, and the client can automatically maintain a synchronized nested object tree (Entry → Room → Player…).

### 4. Reactive Method Support (Reactive)

PinionCore.Remote.Reactive provides Rx extensions to chain remote calls with IObservable<T>.

Important extension methods in PinionCore.Remote.Reactive.Extensions:

- RemoteValue(): Converts Value<T> to IObservable<T>
- SupplyEvent() / UnsupplyEvent(): Converts Notifier to IObservable<T>

In the integration test PinionCore.Integration.Tests/SampleTests.cs:
```csharp
// Important: Rx pattern still requires a background processing loop
var cts = new CancellationTokenSource();
var runTask = Task.Run(async () =>
{
    while (!cts.Token.IsCancellationRequested)
    {
        proxy.Agent.HandlePackets();
        proxy.Agent.HandleMessages();
        await Task.Delay(1, cts.Token);
    }
}, cts.Token);

// Create Rx query chain
var echoObs =
    from e in proxy.Agent
        .QueryNotifier<PinionCore.Remote.Tools.Protocol.Sources.TestCommon.Echoable>()
        .SupplyEvent()
    from val in e.Echo().RemoteValue()
    select val;

var echoValue = await echoObs.FirstAsync();

// Stop background processing
cts.Cancel();
await runTask;
```
This example demonstrates:

- **Background processing loop is necessary**: Even when using Rx, you still need to continuously call HandlePackets/HandleMessages
- Wait for the server to supply the interface through Notifier's SupplyEvent()
- Call remote method Echo() which returns Value<int>
- Use RemoteValue() to convert to IObservable<int>, then get the result once with Rx
### 5. Simple Public and Private Interface Support
Since PinionCore.Remote adopts an interface-oriented design, the server can expose different interfaces to different clients based on requirements. This makes implementing public and private interface requirements simple and intuitive.
For example, you can define a public interface `IPublicService` and a private interface `IPrivateService`:
```csharp
public interface IPublicService  
{
    PinionCore.Remote.Value<string> GetPublicData();
}

public interface IPrivateService : IPublicService
{
    PinionCore.Remote.Value<string> GetPrivateData();
}

class ServiceImpl : IPrivateService
{
    public PinionCore.Remote.Value<string> GetPublicData()
    {
        return "This is public data.";
    }

    public PinionCore.Remote.Value<string> GetPrivateData()
    {
        return "This is private data.";
    }

}
```
The server can decide which interface to bind based on the client's authentication status:
```csharp
void ISessionObserver.OnSessionOpened(ISessionBinder binder)
{
    var serviceImpl = new ServiceImpl();
    if (IsAuthenticatedClient(binder))
    {
        // Bind private interface to authenticated clients
        binder.Bind<IPrivateService>(serviceImpl);
    }

    // Bind public interface to unauthenticated clients
    binder.Bind<IPublicService>(serviceImpl);
}
```
This way, unauthenticated clients can only access `IPublicService`, while authenticated clients can access `IPrivateService`, thus implementing public and private control of interfaces.

### 6. Multi-Transport Modes and Standalone

Built-in three transport methods:

- TCP: PinionCore.Remote.Server.Tcp.ListeningEndpoint / PinionCore.Remote.Client.Tcp.ConnectingEndpoint
- WebSocket: PinionCore.Remote.Server.Web.ListeningEndpoint / PinionCore.Remote.Client.Web.ConnectingEndpoint
- Standalone: PinionCore.Remote.Standalone.ListeningEndpoint (implements both Server and Client endpoints for single-machine simulation)

The integration test SampleTests starts all three endpoints simultaneously and verifies them one by one to ensure consistent behavior across all modes.

---
## Architecture and Module Overview

Main projects:

- PinionCore.Remote: Core interfaces and abstractions (IEntry, ISessionBinder, Value<T>, Property<T>, Notifier<T>, etc.)
- PinionCore.Remote.Client: Proxy, IConnectingEndpoint, and connection extensions (AgentExtensions.Connect)
- PinionCore.Remote.Server: Host, IListeningEndpoint, ServiceExtensions.ListenAsync
- PinionCore.Remote.Soul: Server Session management, update loop (ServiceUpdateLoop)
- PinionCore.Remote.Ghost: Client Agent implementation (User), packet encoding and processing
- PinionCore.Remote.Standalone: ListeningEndpoint simulates Server/Client with memory streams
- PinionCore.Network: IStreamable, TCP/WebSocket Peer, packet read/write
- PinionCore.Serialization: Default serialization implementation and type description
- PinionCore.Remote.Tools.Protocol.Sources: Source Generator, automatically generates IProtocol through [PinionCore.Remote.Protocol.Creator]
- PinionCore.Remote.Gateway: Gateway and multi-service routing (see module README for details)


---
## Quick Start (Hello World)

It is recommended to create three projects: Protocol, Server, and Client. The following examples align with the actual template (PinionCore.Samples.HelloWorld.*) implementation.

### Environment Requirements

- .NET SDK 6 or above
- Visual Studio 2022 / Rider / VS Code
- If Unity is needed, Unity 2021 LTS or above is recommended

### 1. Protocol Project

Create a Class Library:

Sample/Protocol> dotnet new classlib

Add NuGet references (version numbers should match actual releases):
```xml
<ItemGroup>
<PackageReference Include="PinionCore.Remote" Version="0.1.14.15" />
<PackageReference Include="PinionCore.Serialization" Version="0.1.14.12" />
<PackageReference Include="PinionCore.Remote.Tools.Protocol.Sources" Version="0.0.4.25">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
</ItemGroup>
```
Define data structures and interfaces:
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
Create ProtocolCreator (Source Generator entry point):
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

        [PinionCore.Remote.Protocol.Creator]
        static partial void _Create(ref PinionCore.Remote.IProtocol protocol);
    }
}
```

> Note: Methods marked with [PinionCore.Remote.Protocol.Creator] must be
> static partial void Method(ref PinionCore.Remote.IProtocol), otherwise compilation will fail.

### 2. Server Project

Create a Console project:

```Sample/Server> dotnet new console```

csproj references:
```xml
<ItemGroup>
<PackageReference Include="PinionCore.Remote.Server" Version="0.1.14.13" />
<ProjectReference Include="..\Protocol\Protocol.csproj" />
</ItemGroup>
```
Implement IGreeter (aligned with PinionCore.Samples.HelloWorld.Server/Greeter.cs):
```csharp
using Protocol;

namespace Server
{
    class Greeter : IGreeter
    {
        PinionCore.Remote.Value<HelloReply> IGreeter.SayHello(HelloRequest request)
        {
            return new HelloReply { Message = $"Hello {request.Name}." };
        }
    }
}
```
Implement Entry (aligned with HelloWorld example):
```csharp
using PinionCore.Remote;
using Protocol;

namespace Server
{
    class Entry : IEntry
    {
        public volatile bool Enable = true;

        private readonly Greeter _greeter = new Greeter();

        void ISessionObserver.OnSessionOpened(ISessionBinder binder)
        {
            // Client connected successfully, bind IGreeter
            var soul = binder.Bind<IGreeter>(_greeter);
            // To unbind, call binder.Unbind(soul);
        }

        void ISessionObserver.OnSessionClosed(ISessionBinder binder)
        {
            // Cleanup when client disconnects
            Enable = false;
        }

        void IEntry.Update()
        {
            // If server main loop update is needed, put it here
        }
    }
}
```
Start the server main program (following HelloWorld implementation):
```csharp
using System;
using System.Threading.Tasks;
using PinionCore.Remote.Server;
using Protocol;

namespace Server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            int port = int.Parse(args[0]);

            var protocol = ProtocolCreator.Create();
            var entry = new Entry();

            var host = new PinionCore.Remote.Server.Host(entry, protocol);
            PinionCore.Remote.Soul.IService service = host;

            var (disposeServer, errorInfos) = await service.ListenAsync(
                new PinionCore.Remote.Server.Tcp.ListeningEndpoint(port, 10));

            foreach (var error in errorInfos)
            {
                Console.WriteLine($"Listener error: {error.Exception}");
                return;
            }

            Console.WriteLine("Server started.");

            while (entry.Enable)
            {
                System.Threading.Thread.Sleep(0);
                // If needed, you can manually call entry.Update() here
            }

            disposeServer.Dispose();
            host.Dispose();

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
```
### 3. Client Project

Create a Console project:
```
Sample/Client> dotnet new console
```
csproj references:
```xml
<ItemGroup>
<PackageReference Include="PinionCore.Remote.Client" Version="0.1.14.12" />
<PackageReference Include="PinionCore.Remote.Reactive" Version="0.1.14.13" />
<ProjectReference Include="..\Protocol\Protocol.csproj" />
</ItemGroup>
```
Client program (aligned with HelloWorld Client implementation):
```csharp
using System;
using System.Net;
using System.Threading.Tasks;
using PinionCore.Remote.Client;
using Protocol;

namespace Client
{
    internal class Program
    {
        private static bool _enable = true;

        static void Main(string[] args)
        {
            _Run(args).Wait();
        }

        private static async Task _Run(string[] args)
        {
            var ip = IPAddress.Parse(args[0]);
            var port = int.Parse(args[1]);

            var protocol = ProtocolCreator.Create();
            var proxy = new Proxy(protocol);
            var agent = proxy.Agent;

            var endpoint = new PinionCore.Remote.Client.Tcp.ConnectingEndpoint(
                new IPEndPoint(ip, port));

            // Connect() is an extension method in AgentExtensions
            // It automatically calls Enable(stream) and returns IDisposable for cleanup
            // Calling Dispose() will automatically execute Disable() and endpoint.Dispose()
            var connection = await agent.Connect(endpoint).ConfigureAwait(false);

            agent.QueryNotifier<IGreeter>().Supply += greeter =>
            {
                var request = new HelloRequest { Name = "you" };
                greeter.SayHello(request).OnValue += _OnReply;
            };

            // Must continuously process packets and messages, otherwise remote events won't be triggered
            while (_enable)
            {
                System.Threading.Thread.Sleep(0);
                agent.HandleMessages();  // Process messages from remote
                agent.HandlePackets();   // Process packet encoding
            }

            connection.Dispose();
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        private static void _OnReply(HelloReply reply)
        {
            Console.WriteLine($"Receive message: {reply.Message}");
            _enable = false;
        }
    }
}
```

## Core Concepts Explained

### IEntry / ISessionBinder / ISoul

- IEntry: Server entry point, responsible for Session open/close and main loop updates.
- ISessionBinder: Passed in OnSessionOpened, used to Bind<T> / Unbind(ISoul).
- ISoul: Represents an instance bound to a Session, which can later be used for unbinding or querying.

Interface definitions:

- PinionCore.Remote/IEntry.cs
- PinionCore.Remote/ISessionObserver.cs
- PinionCore.Remote/ISessionBinder.cs
- PinionCore.Remote/ISoul.cs

PinionCore.Remote.Soul.Service internally uses SessionEngine to manage all Sessions, while PinionCore.Remote.Server.Host wraps it for convenient service creation.

### Value<T>

Main characteristics of Value<T>:

- Supports OnValue event and await.
- Value is set only once (one-time result).
- Uses implicit conversion: return new HelloReply { ... }; automatically wraps into Value<HelloReply>.

Implementation location: PinionCore.Utility/PinionCore.Utility/Remote/Value.cs

### Property<T>

Property<T> is a notifiable value-type state:

- Changes to the Value property trigger DirtyEvent.
- Can be converted to IObservable<T> through PropertyObservable (PinionCore.Remote.Reactive/PropertyObservable.cs).
- Provides implicit conversion to T, behaves like a normal property.

Implementation location: PinionCore.Remote/Property.cs

### Notifier<T> and Depot<T>

Depot<T> (PinionCore.Utility/Remote/Depot.cs) is a collection + Notifier:

- Items.Add(item): Triggers Supply.
- Items.Remove(item): Triggers Unsupply.
- Notifier<T> wraps Depot<TypeObject>, supporting cross-type querying and event subscription.

The INotifierQueryable interface (PinionCore.Remote/INotifierQueryable.cs) allows calling:

INotifier<T> QueryNotifier<T>();

Ghost.User implements INotifierQueryable, so the client can get the Notifier of any interface through QueryNotifier<T>.

### Streamable Method

If an interface method is defined as follows:
```csharp
PinionCore.Remote.IAwaitableSource<int> StreamEcho(
    byte[] buffer,
    int offset,
    int count);
```
The Source Generator treats it as a "streamable method":

- The transmitted data will only include buffer[offset..offset+count).
- Server-processed data is written back in-place to the same segment.
- The returned IAwaitableSource<int> indicates the actual number of bytes processed (length).

Internal checking logic can be found in PinionCore.Remote.Tools.Protocol.Sources/MethodPinionCoreRemoteStreamable.cs.

---

## Transport Modes and Standalone

### TCP

Server side:
```csharp
var host = new PinionCore.Remote.Server.Host(entry, protocol);
PinionCore.Remote.Soul.IService service = host;
var (disposeServer, errorInfos) = await service.ListenAsync(
    new PinionCore.Remote.Server.Tcp.ListeningEndpoint(port, backlog: 10));
```
Client side:
```csharp
var proxy = new PinionCore.Remote.Client.Proxy(protocol);
using var connection = await proxy.Connect(
    new PinionCore.Remote.Client.Tcp.ConnectingEndpoint(
        new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, port)));
```
### WebSocket

Server side:
```csharp
var (disposeServer, errorInfos) = await service.ListenAsync(
    new PinionCore.Remote.Server.Web.ListeningEndpoint($"http://localhost:{webPort}/"));
```
Client side:
```csharp
var proxy = new PinionCore.Remote.Client.Proxy(protocol);
using var connection = await proxy.Connect(
    new PinionCore.Remote.Client.Web.ConnectingEndpoint(
        $"ws://localhost:{webPort}/"));
```
PinionCore.Remote.Client.Web.ConnectingEndpoint internally uses System.Net.WebSockets.ClientWebSocket and PinionCore.Network.Web.Peer.

### Standalone (Single-Machine Simulation)

PinionCore.Remote.Standalone.ListeningEndpoint implements both:

- PinionCore.Remote.Server.IListeningEndpoint
- PinionCore.Remote.Client.IConnectingEndpoint

Usage (consistent with SampleTests):
```csharp
var protocol = ProtocolCreator.Create();
var entry = new Entry();
var host = new PinionCore.Remote.Server.Host(entry, protocol);
PinionCore.Remote.Soul.IService service = host;

var standaloneEndpoint = new PinionCore.Remote.Standalone.ListeningEndpoint();

var (disposeServer, errors) = await service.ListenAsync(standaloneEndpoint);

var proxy = new PinionCore.Remote.Client.Proxy(protocol);
using var connection = await proxy.Connect(standaloneEndpoint);

// Important: Must continuously process packets and messages
var running = true;
var processTask = Task.Run(async () =>
{
    while (running)
    {
        proxy.Agent.HandlePackets();
        proxy.Agent.HandleMessages();
        await Task.Delay(1);
    }
});

// The rest of the flow is the same as a normal Client
proxy.Agent.QueryNotifier<IGreeter>().Supply += async greeter =>
{
    var reply = await greeter.SayHello(new HelloRequest { Name = "offline" });
    Console.WriteLine(reply.Message);
    running = false;
};

await processTask;

// Cleanup resources
disposeServer.Dispose();
host.Dispose();

// ListeningEndpoint creates a pair of Stream / ReverseStream to simulate send/receive within the same process
```
---
## Advanced Topics

### Reactive Extensions (PinionCore.Remote.Reactive)

PinionCore.Remote.Reactive/Extensions.cs provides the following common extensions:

- ReturnVoid(this Action): Wraps Action into IObservable<Unit>
- RemoteValue(this Value<T>): Converts remote return value to IObservable<T>
- PropertyChangeValue(this Property<T>): Converts property changes to IObservable<T>
- SupplyEvent/UnsupplyEvent(this INotifier<T>): Converts Notifier events to IObservable<T>

SampleTests chains using Rx syntax:

1. Wait for Echo interface to be supplied:

    ```proxy.Agent.QueryNotifier<Echoable>().SupplyEvent()```
2. Call remote Echo() and retrieve with RemoteValue():
```
    from e in ...
    from val in e.Echo().RemoteValue()
    select val;
```
This approach is very suitable when composing multiple consecutive remote calls.

### Gateway Module

PinionCore.Remote.Gateway provides:

- Multi-service entry point (Router)
- Grouping and load balancing (LineAllocator)
- Version coexistence (different IProtocol.VersionCode)
- Gateway main control flow integrated with Chat1 example

For details, refer to PinionCore.Remote.Gateway/README.md and PinionCore.Consoles.Chat1.* projects.

### Custom Connection

If the built-in TCP/WebSocket does not meet your needs, you can implement your own:

- PinionCore.Network.IStreamable (send/receive byte[])
- PinionCore.Remote.Client.IConnectingEndpoint
- PinionCore.Remote.Server.IListeningEndpoint

Usage is the same as built-in endpoints, just with your own protocol or transport at the underlying layer.

### Custom Serialization

If you need custom serialization, use the underlying classes directly instead of simplified wrappers:

**Server side (using PinionCore.Remote.Soul.Service)**:
```csharp
var serializer = new YourSerializer();
var internalSerializer = new YourInternalSerializer();
var pool = PinionCore.Memorys.PoolProvider.Shared;

// Note: Using Soul.Service (underlying full class), not Server.Host (simplified wrapper)
var service = new PinionCore.Remote.Soul.Service(entry, protocol, serializer, internalSerializer, pool);
```

**Client side (using PinionCore.Remote.Ghost.Agent)**:
```csharp
var serializer = new YourSerializer();
var internalSerializer = new YourInternalSerializer();
var pool = PinionCore.Memorys.PoolProvider.Shared;

// Note: Using Ghost.Agent (underlying full class), not Client.Proxy (simplified wrapper)
var agent = new PinionCore.Remote.Ghost.Agent(protocol, serializer, internalSerializer, pool);
```

**Mapping between simplified wrappers and full classes**:
- `Server.Host` internally uses `Soul.Service` with default serialization
- `Client.Proxy` internally uses `Ghost.Agent` with default serialization

Types that need serialization can be obtained from IProtocol.SerializeTypes, or refer to PinionCore.Serialization/README.md.

---
## Examples and Testing

Recommended reading starting from the following projects:

- PinionCore.Samples.Helloworld.Protocols: Basic Protocol and ProtocolCreator implementation
- PinionCore.Samples.Helloworld.Server: Entry, Greeter, Host usage
- PinionCore.Samples.Helloworld.Client: Proxy, ConnectingEndpoint, and QueryNotifier
- **PinionCore.Integration.Tests/SampleTests.cs** (highly recommended):
    - **Starts TCP / WebSocket / Standalone three endpoints simultaneously for parallel testing**
    - Demonstrates how to use Rx (SupplyEvent / RemoteValue) to handle remote calls
    - **Detailed English comments explaining each step**, including why a background processing loop is needed
    - Verifies consistent behavior across all three transport modes
- PinionCore.Remote.Gateway + PinionCore.Consoles.Chat1.*: Gateway real-world implementation case

---

## Conclusion

The goal of PinionCore Remote is to use an "interface-oriented" approach to abstract server-client communication from tedious packet formats and serialization details. You only need to focus on Domain models and state management, while the framework handles connection, serialization, supply/unsupply, and version checking details. Whether it's games, real-time services, tool backends, or connecting multiple services through Gateway, as long as your requirement is "interacting like calling local interfaces between different processes or machines", this framework can be your foundation.

If you're encountering this project for the first time, it's recommended to start with PinionCore.Samples.HelloWorld.*, follow the "Quick Start" section to create Protocol / Server / Client three projects, and run through the complete process. Then read PinionCore.Integration.Tests (especially SampleTests) and Gateway-related examples to better understand how the overall architecture works together in real scenarios. When you need more advanced capabilities, such as custom transport layers or serialization formats, you can refer back to the "Advanced Topics" section and corresponding code files.

During use, if you find the documentation unclear, examples insufficient, or encounter scenarios that actual requirements cannot cover, you are very welcome to open Issues on GitHub for discussion, and welcome to submit PRs. Whether it's supplementary explanations, copy corrections, adding small examples or integration tests, as long as it can make the next user get started more easily, it's a very valuable contribution. Hope that PinionCore Remote can save you time dealing with network details in your projects, allowing you to focus your energy on truly important game and application design.
