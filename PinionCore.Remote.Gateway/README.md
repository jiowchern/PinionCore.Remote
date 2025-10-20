# PinionCore.Remote.Gateway

## 概述

PinionCore.Remote.Gateway 是一個分散式遊戲服務閘道系統，提供客戶端與多個遊戲服務之間的智慧路由與連線管理。它採用三層架構設計，讓客戶端能夠透過單一連接點同時與多個遊戲服務通訊，而無需關心底層的連線細節。

## 核心概念

Gateway 系統由三個主要組件組成：

### 1. Host (閘道服務)
作為中央協調者，負責：
- 接收遊戲服務的註冊（透過 RegistryService）
- 接收客戶端連線（透過 HubService）
- 根據策略將客戶端路由到對應的遊戲服務
- 管理多個遊戲服務的生命週期

### 2. Registry (註冊中心)
作為遊戲服務的註冊代理，負責：
- 向 Host 註冊自己的 Group ID
- 提供 Listener 給遊戲服務，用於接收玩家連線
- 管理遊戲服務與 Host 之間的通訊

### 3. Agent (客戶端代理)
作為玩家客戶端，負責：
- 連接到 Host 的 HubService
- 透過 AgentPool 管理多個遊戲服務的連線
- 使用 CompositeNotifier 整合多個遊戲服務的介面
- 提供統一的 API 給上層應用

## 架構圖

### 整體架構
![整體架構](//www.plantuml.com/plantuml/png/b5F1Qjmm4BthAwO-jCqX5EjOQEcoB9l04iAcjxe7HHaxehPa93bXAVdtIYEjwpNUaFXYQUQzD_D6qW_dXVL3ry9MU7mM1rXP-QWyIZzOPEp30zPA8-mtwv-hc-rk0x8LpZ7M2_b7D0Z5aNTbd2_WBuFmITFrHgFsy2k6VuDmbC_f6UGExPEIU3NS4p3ybr1xR9JpzW0HZfQW8PPsH7XXz5FQews5H2Y2L1QlzEVQM5YtAeYvYo8NojavxrZhbBmL7K8EPOa0p06g7INXylBxN7hiav8Jqijqa1DvA1T6URtHeQTANtTzYrq82Q-Cfa54RidEJQmC1hRM3Fstyk8ujtBkUHUmgD52ISxIjYHRXAVMSvI2UKrngWzVuSvR59suRt4JbR_iK3ozufA_toK_wvAFBldeafzroISk-UYIJrmcRbwFBlcRNAPxjk1krOGVTPSWjC-m9UYjCIt1us89jOvBfppU6AzgbH_66CqFOqQAHd83bTwPHibnlYbA0w6QPFnKRj8vPIUqC8DF0QqrodXgZgMRC_GdrpwCiCWW-5ZfT89eAqt71sKHQVozD5umTj8QqUabpka1II_rmJphLAACsRqPFC9jAmvHzR-DOexchwEPDY5ZLVFemTJ_1YtGdkpT_tpAYkThfpsVklJPYeLFz_G31JMUxEj-kgp9IW5eM5vAK7vIPfucKFZPnbrFryrwiNtzyoaRdyzUzsp5JAxKl1G5aB5GmvFoIqhoSq6c8taPgFDbG__BnhwdqzOzsT6W01DyEg7dsSoc5ntjJvVCUjhG_RAnwrd7fcTpUfzsptpPCEl5YYuaqns0J61r0000)

### 組件詳細架構
![組件詳細架構](//www.plantuml.com/plantuml/png/fLL1Rjim4Bpx5KHESMlb0O9X97KBHq3GkdPo3YXfBH2XI8Ea4WXDJaLFlVGNwGVwfBReBqfI8hIHOZL0VGcrC-JE3dUPKsseCbN9oOeQkACrMSfoBmK8eoUJFSrkQ07aw5ngGtv-UVpp-Vll7zz-FNuz8fycnFuoJhKc7lO1_tkzWO9fe-ejg5kMGKoITjLHpglqC1fTMpWcRTYk4HBERc1G7dgO83NTDgnfAolrl9Gj4pTTF9oFKchGcacnb5BbJ50ZbKlUZw4D6MSWp3lXPIwxZ-UrWi8wBvW0_LuQjcDWyuJLF7706USoeuQc71OedzyJk0vSIiuXCrRWwBuh3ShkUyiybQfwdhnX8WSr8mvQ3FKEoaRQlLDjsgrHr41XoqzMj8GBcQRrjW_s6mRKSQ5ajPyHskcTTgmheKPuM2mJbhIZ6OobnwutO7EidiQaxzhGYOsiHBwHAHCZ1Ng-duvNKOJx6STH8oEAHmvqsJ1CecZnJwl7l9ZVdvoyhAO7z2w4OHbv6m85ktrMW33MPFyNjxus841Pf5bwvmCShTpbnAHYmWolevco_qzoRUi4MXwSfWPsKJmTNPLHTBzenJnnogiOiSdZMFwQYI8cEowfGUhMKlAu8RibGdpCodP5pPkYyQY7T-J3dUZF_6MzXnb9CAEhpslvM86g0pw_N1ox2K7VW-le5BxWHeBoS61FFNg1GtaURuS0qmfQmbf98pFdKXAMGtd-dvnc1b0HJvJcUK2EgKisnMzLAM-X1oCHeaM2M6mKTghno8yvlayS7yXqBNKA8gzA_XS0)


## 時序圖

### 啟動與註冊流程
![](//www.plantuml.com/plantuml/png/XL51YzGm5BxdLppc82FgGgy3bPLbwIY3Y8CtBzdsRGrqafgcNUQc82Axk7fm1KV1WvVriWT1FIZ-6jjn_eL9sZHqH3rC9ydxllU-xtkxXQ9IbOiC8ghmY2vXRXzocEF34dcCXD1O2GcZY2vGlygAnGZ126W1qJmWkOQpcEMKAnZTnPGLIYuXkC_7aHHb7WQNBLHNjh1JKIXRCeU_j37ZKBe6xMq8xGHDomEBql-4X25i7XytppurxvxN9s_gi_VroTbcTTfSV8Om94JBXclNhQG9S3m2Ds-STfADPrPHXQrky_6CFOccSYD5BU0Ip3GLEKeYKPMIRxLtrPsvqhG5FSXmdokK-vLXNmdXbc3ylvGPqWfxYhrPcx_ElzLFZxsrNgd9TD9AtDaJd6Einivb5vatsMx0V3JL1ps1AUL9Xj0rO89xYby7wO_UmXqfAfOWt6ndeZGcV8n-maoaZFlO-yeGvYA_L-ZAeP0G6G84MxFryarw78lRGh53PY8iypnRZisZzsk0HFzwZbFfX5o5bc0nGoKEYWb9SD3mdpcqgxHN6mKNcYLP-a0H-ACAwrVdpUllkjAyUFdpnzlrwSVwuilcqOVrrzLwzGHQE46UWEb3l1BqKlrZjr-ogmdb8liD)


### 客戶端連線與通訊流程
![](//www.plantuml.com/plantuml/png/jLGzR-is5DtrAuXCDj0Q44UZ3b8OWPC2QLCBw9I5iMuS0ZAfafG3RHcB3fsA3XdRfK3H11dQ0E__l3Zv5uyKhqD9bWtZVIpK1y-vlEUUotka3LEcM6HamWpSi9ACCmx2aB4K0cQ6Iq4I-Ba0COCeOZCZ5JauprW9we0mJVnhbDiWVCPpPfa74xO03ECm-Dh4V3jVxzgtpirpARCAu5wQ-oTIUwvzQL4JK4i-WvFYiWBOPtjVQwjfBALAkM16UXcrxoP-2dEkZIf9NE7iPxm5GDS0sb4DJ0kZJ99u1uOYXaRHQ4JUt__v_Cl_gtyUNc__MZtTlTxzzdp_7ncDeiYNddnzY0KU4W4tlfozjmhsdMTyoGoiRV0FHtFxGp9MO7S3h2gtMzeGmeK19LY6bYamXHBuw4Z7mmROI3rh1IhiXnmyPaWsGI5nFCwk8UrTmRkLs5O9vmHCrK5kSHWRPKXEc4WpsBm6WUEQAhABfTGRvwrEs6E1K0DLdhuyFQn-Vtn-VBVwzu-QeJNOHkW4Cfi6UUksGdCn9ubHDlwyl12zgIn4EfMNNFGt8hck7ThLNcDj70dOWlGcIXOvYKF1V7UxnP9SamuHooupE7Rc7ozTS-dgP-1L_Jn90ajxAmRVIIxMQEoJ-bmvriP6vleQKZorhHfJZQCK6absAQJR5DAcGXgvnUTAzrT8hSB0-ZY5TAj2sb98MmhNpNckv9AdK3j3rvarZkzGl2w1U1gOKwcju9qN_mSHu4cHvrc9OkD-zuZmb_HAgc3TdX7flX4fuiBmg1vwWE6O-ug_8jNJHasXgH07IQ0drhRSbcwHIyqDV2yDl-AWBeGxr9Al_U2xOZCWfimXgeLqhk1d62Uj4yCtggbcmEljtSlVl-uSvnCmPs2kPHhtTernbF5Z0QfS9__DgMUwleuFUzk7APAdsBgX25rnrhHQZgVTo0xDdb7NnsuODqHW0FJFNy3-XHZuaNV6HDbRaD4XMGniGNvYMG6s7lqGpy-SsXvPSXRwRsDArTF7Aui39k0mk8DwNuOD3F9tcaO_oJJwcKoZUvj6lx1fj6qQ3QRHfcbqO_1scKQRfj4kqsYNQKSWqcAHVG00)

### 斷線處理流程
![](//www.plantuml.com/plantuml/png/ZP8nJlim5CPtdyBgrF_mB-eQAQYee7Qe16hK0xZ9exBasC5sWde3n04O69cGa62Z0-UXXLkGPmUhXWuCjPo-txE_xtiU6SkqhKj19yp2DLlJKsvo9INabYj9CxYgGUP0IDLoFESLavRs9gm4EXOSfErHPhfjp9oilgkuErIyK4eu03TnVQgCxyLDN9h3YXkhrtJeWFPpU41S1hhXENJG91iIagRq_VbXU_EwUthPtcrsctkQfeHuxVJ_iTUPO0ALCa2Fzh8WhCsZOHOwCsx57fJkMjXIGA2y8u8hGcWYENIQjuSBOcWbQRF4opEhWPK1z3PnQK6qatG4NZi3ri1P0zBsZVlpN-XppjjVw38-bu7h2NW1KKC1yRq5n8AziU3-N8x3Yrb8KrULM8UZOvHXReCkhlxFwXnArG2zrAhqEHkQAo7yoMHV1lBXEqm5HnoUnyVZs-Vj1aSSSllB70AsaDeT1PhEkEnKJUjbX0ND2mFQ8_ZoGF_ER0Iog4ln1G00)

## 快速開始

### 1. 安裝

透過 NuGet 安裝套件：

```bash
dotnet add package PinionCore.Remote.Gateway
```

### 2. 建立 Host

```csharp
using PinionCore.Remote.Gateway;

// 建立 Host
using var host = new Host();

// host.RegistryService - 供遊戲服務註冊使用
// host.HubService - 供客戶端連接使用
```

### 3. 建立遊戲服務與 Registry

```csharp
using PinionCore.Remote.Gateway;
using PinionCore.Remote.Soul;

// 建立遊戲服務
public class MyGameEntry : IEntry
{
    void IBinderProvider.RegisterClientBinder(IBinder binder)
    {
        // 綁定遊戲服務介面
        binder.Bind<IMyGameService>(this);
    }

    void IBinderProvider.UnregisterClientBinder(IBinder binder)
    {
        // 清理
    }

    void IEntry.Update()
    {
        // 遊戲邏輯更新
    }
}

// 建立服務
var gameEntry = new MyGameEntry();
var gameService = PinionCore.Remote.Standalone.Provider.CreateService(
    gameEntry,
    protocol
);

// 建立 Registry (使用 Group ID = 1)
var registry = new Registry(1);

// 啟動 Agent Worker (處理訊息)
var registryWorker = new AgentWorker(registry.Agent);

// 綁定遊戲服務到 Listener
registry.Listener.StreamableEnterEvent += gameService.Join;
registry.Listener.StreamableLeaveEvent += gameService.Leave;

// 連接到 Host
registry.Agent.Connect(host.RegistryService);
```

### 4. 建立客戶端

```csharp
using PinionCore.Remote.Gateway;
using PinionCore.Remote.Gateway.Hosts;

// 建立 Agent (需要提供遊戲協議)
var agent = new Agent(new AgentPool(gameProtocol));

// 啟動 Agent Worker
var agentWorker = new AgentWorker(agent);

// 連接到 Host
agent.Connect(host.HubService);

// 使用 Agent 查詢遊戲服務
var notifier = agent.QueryNotifier<IMyGameService>();

// 監聽服務供應
notifier.Supply += (service) =>
{
    // 可以開始使用 service
    var result = await service.GetData().RemoteValue();
};

// 處理訊息 (在遊戲迴圈中)
agent.HandleMessage();
agent.HandlePackets();
```

## 完整範例

參考測試檔案 `PinionCore.Remote.Gateway.Test/Tests.cs` 中的 `GatewayRegistryAgentIntegrationTestAsync` 方法，這是一個完整的使用範例，展示了：

1. 如何建立 Host
2. 如何建立多個遊戲服務
3. 如何建立多個 Registry 並註冊到 Host
4. 如何建立客戶端並同時與多個遊戲服務通訊

## API 說明

### Host

```csharp
public class Host : IDisposable
{
    // 供遊戲服務註冊使用的端點
    public readonly IService RegistryService;

    // 供客戶端連接使用的端點
    public readonly IService HubService;

    public Host();
    public void Dispose();
}
```

### Registry

```csharp
public class Registry : IDisposable
{
    // 用於連接到 Host 的 Agent
    public readonly IAgent Agent;

    // 供遊戲服務監聽玩家連線的 Listener
    public readonly IListenable Listener;

    // group - 用於 Host 路由決策的群組 ID
    public Registry(uint group);

    public void Dispose();
}
```

### Agent

```csharp
public class Agent : IAgent
{
    // 建立 Agent
    // pool - AgentPool，用於管理多個遊戲服務的連線
    public Agent(AgentPool pool);

    // 查詢遊戲服務介面的 Notifier
    INotifier<T> QueryNotifier<T>();

    // 處理訊息 (需在遊戲迴圈中定期呼叫)
    void HandleMessage();
    void HandlePackets();

    // 連接/斷線
    void Enable(IStreamable streamable);
    void Disable();
}
```

### AgentPool

```csharp
public class AgentPool : IDisposable
{
    // 內部 Agent，用於連接到 Host
    public IAgent Agent { get; }

    // 遊戲服務的 Agent 集合
    public Notifier<IAgent> Agents { get; }

    // gameProtocol - 遊戲協議
    public AgentPool(IProtocol gameProtocol);

    public void Dispose();
}
```

## 進階主題

### 自訂路由策略

Host 預設使用 `RoundRobinGameLobbySelectionStrategy` 進行路由，您可以實作 `IGameLobbySelectionStrategy` 介面來自訂路由邏輯：

```csharp
public interface IGameLobbySelectionStrategy
{
    ILineAllocatable Select(IEnumerable<ILineAllocatable> groups);
}
```

範例：

```csharp
public class CustomStrategy : IGameLobbySelectionStrategy
{
    public ILineAllocatable Select(IEnumerable<ILineAllocatable> groups)
    {
        // 自訂選擇邏輯，例如：
        // - 基於負載
        // - 基於地理位置
        // - 基於玩家偏好
        return groups.First();
    }
}

// 使用自訂策略
var hub = new ServiceHub(new CustomStrategy());
```

### Group ID 的使用

Group ID 是一個重要的概念，用於區分不同的遊戲服務類型或分區：

- **相同 Group ID**: 代表相同類型的服務，Host 會使用策略選擇其中一個
- **不同 Group ID**: 代表不同類型的服務，Host 會將客戶端路由到所有 Group

範例：
```csharp
// 遊戲大廳服務 (Group 1)
var lobbyRegistry = new Registry(1);

// 戰鬥服務 (Group 2)
var battleRegistry = new Registry(2);

// 客戶端連接後，會同時與大廳服務和戰鬥服務建立連線
```

### 使用 Reactive Extensions (Rx)

Gateway 整合了 Reactive Extensions，讓您可以使用流式處理方式處理遊戲服務：

```csharp
// 使用 LINQ 查詢遊戲服務
var observable = from service in agent.QueryNotifier<IMyService>().SupplyEvent()
                 from result in service.GetData().RemoteValue()
                 select result;

var data = await observable.FirstAsync();
```

### 網路模式 vs 單機模式

範例使用的是 Standalone 模式（單機模式），適合開發和測試。生產環境應使用網路模式：

```csharp
// 網路模式 - Server
var service = Provider.CreateTcpService(entry, protocol, port);

// 網路模式 - Client
var agent = Provider.CreateTcpAgent(protocol);
agent.Connect(host, port);
```

## 注意事項

1. **Worker 的重要性**: Registry 和 Agent 都需要使用 `AgentWorker` 來處理訊息，確保定期呼叫 `HandleMessage()` 和 `HandlePackets()`

2. **資源釋放**: 所有組件都實作了 `IDisposable`，請確保適當釋放資源

3. **Group ID 規劃**: 合理規劃 Group ID 可以讓路由更有效率，避免不必要的連線

4. **策略選擇**: 選擇適合的路由策略可以提升系統效能和玩家體驗

5. **錯誤處理**: 監聽 Agent 的錯誤事件以處理網路異常：
   ```csharp
   agent.ExceptionEvent += (ex) => Console.WriteLine($"Error: {ex}");
   agent.ErrorMethodEvent += (method, msg) => Console.WriteLine($"Method Error: {method} - {msg}");
   ```

## 相關資源

- [PinionCore.Remote 核心文件](../README.md)
- [Protocol 程式碼產生器](../PinionCore.Remote.Tools.Protocol.Sources/README.md)
- [範例專案](../PinionCore.Samples.HelloWorld.Client/README.md)

## 授權

MIT License
