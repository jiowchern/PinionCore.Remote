# 傳輸模式與 Standalone

[上一節：核心概念](core-concepts.md) | [下一節：進階主題](advanced-topics.md)

## TCP

伺服器端：

```csharp
var host = new PinionCore.Remote.Server.Host(entry, protocol);
PinionCore.Remote.Soul.IService service = host;

var (disposeServer, errorInfos) = await service.ListenAsync(
    new PinionCore.Remote.Server.Tcp.ListeningEndpoint(port, backlog: 10));
```

客戶端：

```csharp
var proxy = new PinionCore.Remote.Client.Proxy(protocol);
using var connection = await proxy.Connect(
    new PinionCore.Remote.Client.Tcp.ConnectingEndpoint(
        new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, port)));
```

---

## WebSocket

伺服器端：

```csharp
var (disposeServer, errorInfos) = await service.ListenAsync(
    new PinionCore.Remote.Server.Web.ListeningEndpoint($"http://localhost:{webPort}/"));
```

客戶端：

```csharp
var proxy = new PinionCore.Remote.Client.Proxy(protocol);
using var connection = await proxy.Connect(
    new PinionCore.Remote.Client.Web.ConnectingEndpoint(
        $"ws://localhost:{webPort}/"));
```

---

## Standalone（單機模擬）

`PinionCore.Remote.Standalone.ListeningEndpoint` 同時實作 `PinionCore.Remote.Server.IListeningEndpoint` 與 `PinionCore.Remote.Client.IConnectingEndpoint`，可在同一進程模擬 Server/Client。

```csharp
var protocol = ProtocolCreator.Create();
var entry = new Entry();
var host = new PinionCore.Remote.Server.Host(entry, protocol);
PinionCore.Remote.Soul.IService service = host;

var standaloneEndpoint = new PinionCore.Remote.Standalone.ListeningEndpoint();

var (disposeServer, errors) = await service.ListenAsync(standaloneEndpoint);

var proxy = new PinionCore.Remote.Client.Proxy(protocol);
using var connection = await proxy.Connect(standaloneEndpoint);

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
```

- 適合單元測試、離線模擬與整合測試。
- 行為與 TCP/WebSocket 保持一致。
