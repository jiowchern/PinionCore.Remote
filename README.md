———

## Table of Contents

- Introduction (#introduction)
- Key Features (#key-features)
    - Interface-Oriented Communication (#1-interface-oriented-communication)
    - Controllable Lifetime (Entry / Session / Soul) (#2-controllable-lifetime-entry--session--soul)
    - Value / Property / Notifier (#3-value--property--notifier)
    - Reactive Support (#4-reactive-support)
    - Public vs Private Interfaces (#5-simple-public-and-private-interfaces)
    - Multiple Transports & Standalone (#6-multiple-transports-and-standalone)
- Architecture & Modules Overview (#architecture--modules-overview)
- Quick Start (Hello World) (#quick-start-hello-world)
    - Environment Requirements (#environment-requirements)
    - 1. Protocol Project (#1-protocol-project)
    - 2. Server Project (#2-server-project)
    - 3. Client Project (#3-client-project)
- Core Concepts in Detail (#core-concepts-in-detail)
    - IEntry / ISessionBinder / ISoul (#ientry--isessionbinder--isoul-1)
    - Value<T> (#valuet)
    - Property<T> (#propertyt)
    - Notifier<T> & Depot<T> (#notifiert--depott)
    - Streamable Methods (#streamable-methods)
- Transports & Standalone (#transports--standalone)
    - TCP (#tcp)
    - WebSocket (#websocket)
    - Standalone (Offline Simulation) (#standalone-offline-simulation)
- Advanced Topics (#advanced-topics)
    - Reactive Extensions (#reactive-extensions-pinioncoreremotereactive)
    - Gateway Module (#gateway-module)
    - Custom Connection (#custom-connection)
    - Custom Serialization (#custom-serialization)
- Samples & Tests (#samples--tests)
- Closing Words (#closing-words)

———

## Introduction

PinionCore Remote is an interface‑oriented remote communication framework written in C#.

You define your remote protocol using interfaces, the server implements these interfaces, and the client calls them as if they were local objects. The actual data is transported underneath via TCP /
WebSocket / Standalone (single‑process simulation).

- Supports .NET Standard 2.1 (.NET 6/7/8, Unity 2021+).
- Supports IL2CPP and AOT (requires pre‑registering serializable types).
- Built‑in TCP, WebSocket, Standalone transport modes.
- Uses a Source Generator to automatically generate the IProtocol implementation, lowering maintenance cost.
- Uses Value / Property / Notifier as core abstractions to describe remote behavior and state.
- With PinionCore.Remote.Reactive, you can write remote flows in Rx style.

———

## Online Docs

- DeepWiki (https://deepwiki.com/jiowchern/PinionCore.Remote)
- OpenDeepWiki (https://opendeep.wiki/jiowchern/PinionCore.Remote/introduction?branch=master)

———

## Key Features

### 1. Interface-Oriented Communication

You only need to define interfaces; no need to hand‑write serialization or protocol parsing:

public interface IGreeter
{
    PinionCore.Remote.Value<HelloReply> SayHello(HelloRequest request);
}

Server implementation:

class Greeter : IGreeter
{
    PinionCore.Remote.Value<HelloReply> IGreeter.SayHello(HelloRequest request)
    {
        return new HelloReply { Message = $"Hello {request.Name}." };
    }
}

On the client, you obtain a remote proxy through QueryNotifier<IGreeter>() and call it like a local object:

agent.QueryNotifier<IGreeter>().Supply += greeter =>
{
    var request = new HelloRequest { Name = "you" };
    greeter.SayHello(request).OnValue += reply =>
    {
        Console.WriteLine($"Receive message: {reply.Message}");
    };
};

- Value<T> can be await‑ed, or you can subscribe with the OnValue event.
- You do not need to manage any connection ID or RPC ID; you just follow the interface model.

———

### 2. Controllable Lifetime (Entry / Session / Soul)

On the server, the entry point implements PinionCore.Remote.IEntry. The framework calls it when sessions are opened or closed:

public class Entry : PinionCore.Remote.IEntry
{
    private readonly Greeter _greeter = new Greeter();

    void PinionCore.Remote.ISessionObserver.OnSessionOpened(PinionCore.Remote.ISessionBinder binder)
    {
        // Client connected: bind _greeter
        var soul = binder.Bind<IGreeter>(_greeter);

        // To unbind later:
        // binder.Unbind(soul);
    }

    void PinionCore.Remote.ISessionObserver.OnSessionClosed(PinionCore.Remote.ISessionBinder binder)
    {
        // Cleanup when the client disconnects
    }

    void PinionCore.Remote.IEntry.Update()
    {
        // Per‑tick update for the entry (optional)
    }
}

Creating the host:

var host = new PinionCore.Remote.Server.Host(entry, protocol);
// Host internally uses SessionEngine to manage all sessions.

Entry / Session / Soul together form a controllable lifetime model for remote objects.

———

### 3. Value / Property / Notifier

PinionCore Remote centers around “interfaces” and provides three common member types to describe remote behavior and state:

#### Value<T>: One‑Shot Async Call

- Similar in concept to Task<T>.
- Used for request/response flows such as login, fetching settings, sending commands.
- Set exactly once; supports await and the OnValue event.

Value<LoginResult> Login(LoginRequest request);

#### Property<T>: Stable Remote State

- The server side maintains the actual value.
- Clients read it via proxies, and can receive notifications when it changes (DirtyEvent / Observable).
- Suitable for things like player name, room title, server version, etc.

Property<string> Nickname { get; }
Property<string> RoomName { get; }

#### Notifier<T>: Dynamic Collections and Hierarchies

INotifier<T> describes “a set of remote objects that come and go dynamically”. T itself can be an interface, so it is well suited to modeling hierarchical structures like Lobby / Room / Player:

public interface IChatEntry
{
    INotifier<IRoom> Rooms { get; }
}

public interface IRoom
{
    Property<string> Name { get; }
    INotifier<IPlayer> Players { get; }
}

public interface IPlayer
{
    Property<string> Nickname { get; }
}

Server:

- Room created → Rooms.Supply(roomImpl)
- Room removed → Rooms.Unsupply(roomImpl)
- Player joins → room.Players.Supply(playerImpl)
- Player leaves → room.Players.Unsupply(playerImpl)

Client:

agent.QueryNotifier<IRoom>().Supply += room =>
{
    // room is already a remote proxy
    room.Players.Supply += player =>
    {
        Console.WriteLine($"Player joined: {player.Nickname.Value}");
    };
};

Key points:

- A Notifier is not just an event set; it is a dynamic collection of objects that grow and shrink, and a tool to keep hierarchical object graphs (trees) in sync.
- The client never manages IDs or lookup tables; it only navigates via interface layers.

———

### 4. Reactive Support (Reactive)

PinionCore.Remote.Reactive provides Rx extensions so you can compose remote flows via IObservable<T>.

Key extensions (in PinionCore.Remote.Reactive.Extensions):

- RemoteValue() – Value<T> → IObservable<T>
- SupplyEvent() / UnsupplyEvent() – INotifier<T> → IObservable<T>

Example from the integration test PinionCore.Integration.Tests/SampleTests.cs:

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

var echoObs =
    from e in proxy.Agent
        .QueryNotifier<Echoable>()
        .SupplyEvent()
    from val in e.Echo().RemoteValue()
    select val;

var echoValue = await echoObs.FirstAsync();

cts.Cancel();
await runTask;

Notes:

- Even with Rx, a background processing loop is still required (you must keep calling HandlePackets and HandleMessages).
- Rx only makes composition easier; it does not replace the underlying event processing.

———

### 5. Simple Public and Private Interfaces

Because the framework is interface‑oriented, the server can bind different interfaces to different clients and easily express “public vs private” APIs:

public interface IPublicService
{
    Value<string> GetPublicData();
}

public interface IPrivateService : IPublicService
{
    Value<string> GetPrivateData();
}

class ServiceImpl : IPrivateService
{
    public Value<string> GetPublicData() => "This is public data.";
    public Value<string> GetPrivateData() => "This is private data.";
}

Server:

void ISessionObserver.OnSessionOpened(ISessionBinder binder)
{
    var serviceImpl = new ServiceImpl();

    if (IsAuthenticatedClient(binder))
    {
        // Authenticated client
        binder.Bind<IPrivateService>(serviceImpl);
    }

    // Everyone gets the public service
    binder.Bind<IPublicService>(serviceImpl);
}

Unauthenticated clients can only access IPublicService; authenticated ones can also use IPrivateService.

———

### 6. Multiple Transports and Standalone

Three built‑in transports:

- TCP
    - PinionCore.Remote.Server.Tcp.ListeningEndpoint
    - PinionCore.Remote.Client.Tcp.ConnectingEndpoint
- WebSocket
    - PinionCore.Remote.Server.Web.ListeningEndpoint
    - PinionCore.Remote.Client.Web.ConnectingEndpoint
- Standalone (single‑process simulation)
    - PinionCore.Remote.Standalone.ListeningEndpoint
    - Implements both server and client endpoints, suitable for same‑process simulation and tests.

The integration tests in SampleTests start all three endpoints and verify them one by one to ensure behavior consistency.

———

## Architecture & Modules Overview

Main projects and roles:

- PinionCore.Remote
    - Core interfaces and abstractions: IEntry, ISessionBinder, ISoul.
    - State types: Value<T>, Property<T>, Notifier<T>.
- PinionCore.Remote.Client
    - Proxy, IConnectingEndpoint.
    - Connection helpers: AgentExtensions.Connect.
- PinionCore.Remote.Server
    - Host, IListeningEndpoint.
    - Starting services and listening: ServiceExtensions.ListenAsync.
- PinionCore.Remote.Soul
    - Server‑side session management (SessionEngine).
    - Update loop: ServiceUpdateLoop.
- PinionCore.Remote.Ghost
    - Client‑side Agent implementation (User).
    - Packet encoding, decoding and handling.
- PinionCore.Remote.Standalone
    - ListeningEndpoint that uses in‑memory streams to simulate server/client.
- PinionCore.Network
    - IStreamable abstraction, TCP/WebSocket peers, packet read/write.
- PinionCore.Serialization
    - Default serialization implementation and type descriptors (can be replaced).
- PinionCore.Remote.Tools.Protocol.Sources
    - Source Generator.
    - Uses [PinionCore.Remote.Protocol.Creator] to auto‑generate IProtocol.
- PinionCore.Remote.Gateway
    - Gateway/Router module, multi‑service routing and version coexistence
    (see that project’s README for details).

———

## Quick Start (Hello World)

It is recommended to create three projects: Protocol, Server, Client.
The following is a simplified walkthrough; see the sample projects in the repo for complete versions:

- PinionCore.Samples.HelloWorld.Protocols
- PinionCore.Samples.HelloWorld.Server
- PinionCore.Samples.HelloWorld.Client

### Environment Requirements

- .NET SDK 6 or later.
- Visual Studio 2022 / Rider / VS Code.
- For Unity: Unity 2021 LTS or later is recommended.

———

### 1. Protocol Project

Create a class library:

Sample/Protocol> dotnet new classlib

Add NuGet references (version numbers here are examples):

<ItemGroup>
<PackageReference Include="PinionCore.Remote" Version="0.1.14.15" />
<PackageReference Include="PinionCore.Serialization" Version="0.1.14.12" />
<PackageReference Include="PinionCore.Remote.Tools.Protocol.Sources" Version="0.0.4.25">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
</ItemGroup>

Define data types and interfaces (simplified HelloWorld):

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

Define ProtocolCreator (entry for the Source Generator):

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

> Note: Methods marked with [PinionCore.Remote.Protocol.Creator] must have the signature
> static partial void Method(ref PinionCore.Remote.IProtocol); otherwise the project will not compile.

———

### 2. Server Project

Create a console application:

Sample/Server> dotnet new console

csproj example:

<ItemGroup>
<PackageReference Include="PinionCore.Remote.Server" Version="0.1.14.13" />
<ProjectReference Include="..\Protocol\Protocol.csproj" />
</ItemGroup>

Implement IGreeter:

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

Implement Entry:

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
            // Client connected: bind IGreeter
            var soul = binder.Bind<IGreeter>(_greeter);
        }

        void ISessionObserver.OnSessionClosed(ISessionBinder binder)
        {
            // Cleanup when the client disconnects
            Enable = false;
        }

        void IEntry.Update()
        {
            // Main server update loop work (if needed)
        }
    }
}

Server program (TCP version):

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
                new PinionCore.Remote.Server.Tcp.ListeningEndpoint(port, backlog: 10));

            foreach (var error in errorInfos)
            {
                Console.WriteLine($"Listener error: {error.Exception}");
                return;
            }

            Console.WriteLine("Server started.");

            while (entry.Enable)
            {
                System.Threading.Thread.Sleep(0);
                // You may also call entry.Update() here if needed.
            }

            disposeServer.Dispose();
            host.Dispose();

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}

———

### 3. Client Project

Create a console application:

Sample/Client> dotnet new console

csproj example:

<ItemGroup>
<PackageReference Include="PinionCore.Remote.Client" Version="0.1.14.12" />
<PackageReference Include="PinionCore.Remote.Reactive" Version="0.1.14.13" />
<ProjectReference Include="..\Protocol\Protocol.csproj" />
</ItemGroup>

Client program (simplified):

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

            // Connect() is an extension method from AgentExtensions
            var connection = await agent.Connect(endpoint).ConfigureAwait(false);

            agent.QueryNotifier<IGreeter>().Supply += greeter =>
            {
                var request = new HelloRequest { Name = "you" };
                greeter.SayHello(request).OnValue += _OnReply;
            };

            // Must keep processing packets and messages
            while (_enable)
            {
                System.Threading.Thread.Sleep(0);
                agent.HandleMessages();
                agent.HandlePackets();
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

———

## Core Concepts in Detail

### IEntry / ISessionBinder / ISoul

- IEntry: Server entry point, responsible for session open/close and updates.
- ISessionBinder: Passed in OnSessionOpened, used for Bind<T> / Unbind(ISoul).
- ISoul: Represents an instance already bound to a session; you can use it later to unbind or look it up.

Relevant files:

- PinionCore.Remote/IEntry.cs
- PinionCore.Remote/ISessionObserver.cs
- PinionCore.Remote/ISessionBinder.cs
- PinionCore.Remote/ISoul.cs

PinionCore.Remote.Soul.Service uses SessionEngine to manage all sessions,
while PinionCore.Remote.Server.Host wraps it to make service creation easier.

———

### Value<T>

Characteristics:

- Supports the OnValue event and await.
- Set exactly once (one‑shot result).
- Supports implicit conversion: return new HelloReply { ... }; automatically wraps into Value<HelloReply>.

Implementation: PinionCore.Utility/Remote/Value.cs.

———

### Property<T>

A state value that can notify on changes:

- Setting Value raises a DirtyEvent.
- Can be turned into IObservable<T> via PropertyObservable (see PinionCore.Remote.Reactive/PropertyObservable.cs).
- Supports implicit conversion to T so you can use it like a normal property.

Implementation: PinionCore.Remote/Property.cs.

———

### Notifier<T> & Depot<T>

Depot<T> (PinionCore.Utility/Remote/Depot.cs) combines a collection and notifications:

- Items.Add(item) → triggers Supply.
- Items.Remove(item) → triggers Unsupply.

Notifier<T> wraps Depot<TypeObject>, supporting cross‑type queries and event subscriptions.

INotifierQueryable (in PinionCore.Remote/INotifierQueryable.cs) provides:

INotifier<T> QueryNotifier<T>();

Ghost.User implements INotifierQueryable,
so the client can get a Notifier for any interface via QueryNotifier<T>.

———

### Streamable Methods

If an interface method is defined like this:

PinionCore.Remote.IAwaitableSource<int> StreamEcho(
    byte[] buffer,
    int offset,
    int count);

The Source Generator treats it as a streamable method:

- Only buffer[offset..offset+count) is sent over the wire.
- The server writes the processed data back into the same region.
- The returned IAwaitableSource<int> is the number of bytes actually processed.

Implementation details:
PinionCore.Remote.Tools.Protocol.Sources/MethodPinionCoreRemoteStreamable.cs.

———

## Transports & Standalone

### TCP

Server:

var host = new PinionCore.Remote.Server.Host(entry, protocol);
PinionCore.Remote.Soul.IService service = host;

var (disposeServer, errorInfos) = await service.ListenAsync(
    new PinionCore.Remote.Server.Tcp.ListeningEndpoint(port, backlog: 10));

Client:

var proxy = new PinionCore.Remote.Client.Proxy(protocol);
using var connection = await proxy.Connect(
    new PinionCore.Remote.Client.Tcp.ConnectingEndpoint(
        new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, port)));

———

### WebSocket

Server:

var (disposeServer, errorInfos) = await service.ListenAsync(
    new PinionCore.Remote.Server.Web.ListeningEndpoint($"http://localhost:{webPort}/"));

Client:

var proxy = new PinionCore.Remote.Client.Proxy(protocol);
using var connection = await proxy.Connect(
    new PinionCore.Remote.Client.Web.ConnectingEndpoint(
        $"ws://localhost:{webPort}/"));

———

### Standalone (Offline Simulation)

PinionCore.Remote.Standalone.ListeningEndpoint simultaneously implements:

- PinionCore.Remote.Server.IListeningEndpoint
- PinionCore.Remote.Client.IConnectingEndpoint

Usage (simplified from SampleTests):

var protocol = ProtocolCreator.Create();
var entry = new Entry();
var host = new PinionCore.Remote.Server.Host(entry, protocol);
PinionCore.Remote.Soul.IService service = host;

var standaloneEndpoint = new PinionCore.Remote.Standalone.ListeningEndpoint();

var (disposeServer, errors) = await service.ListenAsync(standaloneEndpoint);

var proxy = new PinionCore.Remote.Client.Proxy(protocol);
using var connection = await proxy.Connect(standaloneEndpoint);

// Important: must keep processing packets and messages
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

proxy.Agent.QueryNotifier<IGreeter>().Supply += async greeter =>
{
    var reply = await greeter.SayHello(new HelloRequest { Name = "offline" });
    Console.WriteLine(reply.Message);
    running = false;
};

await processTask;

disposeServer.Dispose();
host.Dispose();

———

## Advanced Topics

### Reactive Extensions (PinionCore.Remote.Reactive)

PinionCore.Remote.Reactive/Extensions.cs provides commonly used helpers:

- ReturnVoid(this Action) → Action → IObservable<Unit>
- RemoteValue(this Value<T>) → convert a remote return value into IObservable<T>
- PropertyChangeValue(this Property<T>) → property changes as IObservable<T>
- SupplyEvent/UnsupplyEvent(this INotifier<T>) → Notifier events as IObservable<T>

With LINQ‑to‑Rx, you can naturally compose remote workflows.

———

### Gateway Module

PinionCore.Remote.Gateway provides:

- Multi‑service entry (Router).
- Grouping and load‑balancing (LineAllocator).
- Protocol version coexistence (multiple IProtocol.VersionCode).
- Example gateway integrated with PinionCore.Consoles.Chat1.*.

See PinionCore.Remote.Gateway/README.md and the Chat samples for details.

———

### Custom Connection

If the built‑in TCP / WebSocket transports are not suitable, you can customize:

- PinionCore.Network.IStreamable (sending/receiving byte[]).
- PinionCore.Remote.Client.IConnectingEndpoint.
- PinionCore.Remote.Server.IListeningEndpoint.

Usage is the same as built‑in endpoints; only the underlying protocol/transport is replaced by your own.

———

### Custom Serialization

When you need custom serialization, it is recommended to use the low‑level types directly (rather than wrapper helpers).

Server side (using Soul.Service):

var serializer = new YourSerializer();
var internalSerializer = new YourInternalSerializer();
var pool = PinionCore.Memorys.PoolProvider.Shared;

var service = new PinionCore.Remote.Soul.Service(
    entry, protocol, serializer, internalSerializer, pool);

Client side (using Ghost.Agent):

var serializer = new YourSerializer();
var internalSerializer = new YourInternalSerializer();
var pool = PinionCore.Memorys.PoolProvider.Shared;

var agent = new PinionCore.Remote.Ghost.Agent(
    protocol, serializer, internalSerializer, pool);

Relationships:

- Server.Host wraps a Soul.Service with the default serializer.
- Client.Proxy wraps a Ghost.Agent with the default serializer.

The set of types that need serialization can be retrieved from IProtocol.SerializeTypes,
or see PinionCore.Serialization/README.md.

———

## Samples & Tests

Recommended reading order:

1. PinionCore.Samples.HelloWorld.Protocols
    - Basic protocol and ProtocolCreator implementation.
2. PinionCore.Samples.HelloWorld.Server
    - Usage of Entry, Greeter, and Host.
3. PinionCore.Samples.HelloWorld.Client
    - Usage of Proxy, ConnectingEndpoint, and QueryNotifier.
4. PinionCore.Integration.Tests/SampleTests.cs (strongly recommended)
    - Starts TCP / WebSocket / Standalone endpoints simultaneously.
    - Uses Rx (SupplyEvent, RemoteValue) to handle remote calls.
    - Detailed English comments explain why the background processing loop is necessary.
    - Verifies behavior consistency across different transports.
5. PinionCore.Remote.Gateway + PinionCore.Consoles.Chat1.*
    - How Gateway is composed and operates in a real project.

———

## Closing Words

The design goal of PinionCore Remote is to use an interface‑oriented approach to lift server‑client communication out of the low‑level details of packet formats, serialization, and ID management. You focus
on your domain model and state management, while the framework takes care of connections, supply/unsupply, version checks, and other plumbing.

Whether you are writing games, real‑time services, tooling backends, or orchestrating multiple services through a Gateway, whenever your requirement is “talk across processes or machines as if calling local
interfaces”, this framework can be your foundation.

If this is your first time using the project, we recommend:

1. Follow Quick Start to build the Protocol / Server / Client projects and run the Hello World.
2. Then read through PinionCore.Integration.Tests (especially SampleTests) and the Gateway samples.
3. When you need more advanced features, come back to the Advanced Topics section and the corresponding code files.

If you find any unclear parts in the documentation, gaps in the samples, or run into special needs, please feel free to open an Issue on GitHub. PRs are also very welcome—clarifying docs, fixing wording,
adding small samples or integration tests—all of these help the next user get productive faster.

We hope PinionCore Remote can save you from the details of network plumbing and let you focus on what really matters: the design of your game and application.
