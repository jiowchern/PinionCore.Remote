# Transport Modes & Standalone

[Back: Core Concepts](core-concepts.md) | [Next: Advanced Topics](advanced-topics.md)

## TCP

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

---

## WebSocket

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

---

## Standalone (In-Process Simulation)

`PinionCore.Remote.Standalone.ListeningEndpoint` implements both `PinionCore.Remote.Server.IListeningEndpoint` and `PinionCore.Remote.Client.IConnectingEndpoint`, allowing server & client simulation within the same process.

Example (simplified from `SampleTests`):

```csharp
var protocol = ProtocolCreator.Create();
var entry = new Entry();
var host = new PinionCore.Remote.Server.Host(entry, protocol);
PinionCore.Remote.Soul.IService service = host;

var standaloneEndpoint = new PinionCore.Remote.Standalone.ListeningEndpoint();

var (disposeServer, errors) = await service.ListenAsync(standaloneEndpoint);

var proxy = new PinionCore.Remote.Client.Proxy(protocol);
using var connection = await proxy.Connect(standaloneEndpoint);

// Background processing loop
var cts = new CancellationTokenSource();
var processTask = Task.Run(async () =>
{
    while (!cts.Token.IsCancellationRequested)
    {
        proxy.Agent.HandlePackets();
        proxy.Agent.HandleMessages();
        await Task.Delay(1, cts.Token);
    }
}, cts.Token);

cts.Cancel();
await processTask;

disposeServer.Dispose();
host.Dispose();
```

Notes:

- Standalone is perfect for **unit tests**, **offline simulations**, and **integration tests**.
- All behaviors follow the same semantics as TCP/WebSocket.

---

## DirectStandalone (Zero-Serialization In-Process Mode)

`PinionCore.Remote.Standalone.DirectStandalone` is an even lighter in-process mode: it implements both the client-side `Ghost.IAgent` and the server-side `ISessionBinder`, supplying bound Soul instances to `QueryNotifier<T>` **without any serialization**. The object the client receives through `Supply` is the server-side instance itself (shared reference) — subsequent method calls, events, `Property<T>`, and `Spirit<T>` are all plain .NET calls.

```csharp
// No protocol needed — IProtocol, serialization, and generated Ghost code are bypassed entirely
var entry = new Entry();
var direct = new PinionCore.Remote.Standalone.DirectStandalone(entry);
PinionCore.Remote.Ghost.IAgent agent = direct;

agent.QueryNotifier<IGreeter>().Supply += async greeter =>
{
    // greeter IS the server-side instance (same reference)
    var reply = await greeter.SayHello(new HelloRequest { Name = "offline" });
    Console.WriteLine(reply.Message);
};

direct.Launch();  // raises entry.OnSessionOpened(binder)

// Main loop, same as network modes
agent.HandlePackets();   // drives IEntry.Update() (no separate driver needed)
agent.HandleMessages();  // fires queued Supply/Unsupply

direct.Shutdown();  // raises OnSessionClosed and unsupplies all souls immediately
```

### Differences from Standalone (ListeningEndpoint)

| Aspect | Standalone | DirectStandalone |
|--------|-----------|------------------|
| Serialization pipeline | Fully executed | Bypassed entirely |
| IProtocol / code generation | Required | Not needed (the Spirit interface alone suffices) |
| Object semantics | Serialized copies | Shared references |
| Per-call overhead | Serialization + packets | Near zero |
| Serializability validation | Validated | Not validated |

### Timing semantics

To stay consistent with the network modes, `Supply`/`Unsupply` caused by `Bind`/`Unbind` are not raised immediately — they are queued and fired in order during `HandleMessages()`. `Shutdown()` mirrors the synchronous semantics of `Disable` in network mode: all souls are unsupplied immediately.

### Caveats

- **No serializability validation**: a Spirit interface that works here is not guaranteed to be remotable (e.g. passing non-serializable types will still work). Run integration tests with Standalone or TCP before shipping.
- Objects are shared references: client-side mutation of arrays or custom types directly affects server-side state.
- Objects passed to `Return` do not appear in `QueryNotifier`, matching remote-mode semantics.
- Positioned as a **fast-iteration tool** (unit tests, Unity Editor debugging) — a complement to Standalone, not a replacement.
