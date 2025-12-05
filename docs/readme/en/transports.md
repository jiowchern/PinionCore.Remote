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
