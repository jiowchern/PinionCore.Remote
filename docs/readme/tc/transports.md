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

---

## DirectStandalone（直通單機模式）

`PinionCore.Remote.Standalone.DirectStandalone` 是更輕量的單機模式：同時實作客戶端 `Ghost.IAgent` 與伺服器端 `ISessionBinder`，將 `Bind` 進來的 Soul 實例**不經序列化**直接供給到 `QueryNotifier<T>`。客戶端經 `Supply` 取得的物件就是伺服器端實例本身（共用參考），之後的方法呼叫、事件、`Property<T>`、`Spirit<T>` 都是直接的 .NET 呼叫。

```csharp
// 不需要 protocol——IProtocol、序列化、Ghost 生成程式碼完全不使用
var entry = new Entry();
var direct = new PinionCore.Remote.Standalone.DirectStandalone(entry);
PinionCore.Remote.Ghost.IAgent agent = direct;

agent.QueryNotifier<IGreeter>().Supply += async greeter =>
{
    // greeter 就是伺服器端實例本身（同一參考）
    var reply = await greeter.SayHello(new HelloRequest { Name = "offline" });
    Console.WriteLine(reply.Message);
};

direct.Launch();  // 觸發 entry.OnSessionOpened(binder)

// 主迴圈與網路模式相同
agent.HandlePackets();   // 驅動 IEntry.Update()（不需另外驅動）
agent.HandleMessages();  // 觸發佇列中的 Supply/Unsupply

direct.Shutdown();  // 觸發 OnSessionClosed 並立即撤銷所有 Soul
```

### 與 Standalone（ListeningEndpoint）的差異

| 項目 | Standalone | DirectStandalone |
|------|-----------|------------------|
| 序列化管線 | 完整執行 | 完全繞過 |
| IProtocol / 程式碼產生 | 必要 | 不需要（Spirit 介面存在即可） |
| 物件語意 | 序列化複本 | 共用參考 |
| 每次呼叫開銷 | 序列化 + 封包 | 趨近於零 |
| 可序列化性驗證 | 會驗證 | 不驗證 |

### 時序語意

為與網路模式一致，`Bind`/`Unbind` 造成的 `Supply`/`Unsupply` 不會立即觸發，而是排入佇列、於 `HandleMessages()` 時依序發生；`Shutdown()` 則比照網路模式 `Disable` 的同步語意，立即撤銷所有 Soul。

### 注意事項

- **本模式不驗證可序列化性**：在此能運作的 Spirit 介面不代表可遠端化（例如傳遞不可序列化的型別也能正常執行）。上線前仍應以 Standalone 或 TCP 模式進行整合測試。
- 物件是共用參考：客戶端對 array 或自訂型別的改動會直接影響伺服器端狀態。
- `Return` 的物件比照遠端模式不進 `QueryNotifier`。
- 定位為**快速迭代工具**（單元測試、Unity Editor 除錯），是 Standalone 的補充而非取代。
