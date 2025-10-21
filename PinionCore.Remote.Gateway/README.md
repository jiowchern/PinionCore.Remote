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
![整體架構](https://www.plantuml.com/plantuml/svg/b5F1Qjmm4BthAwO-jCqX5EbOQ6bObarW2M7JMzs3ueoDg2qPIKvOIlwzqaXn7U4DYI-MPjvxCsya_N1UMZyDFVjmZwtI28VjvkV5zMw_XQQNpZ4sokQFx12gdVJuA8zmQPolu2-3y3Lc68r6xU5N3Fy6wIYVqmNa5ks3Ql1okYDW-A-X3jWfv-qH8XmjGKCixOZmnEYdj4VRoWQX2b9PlDQVMsDXqwiWviwgN2XdIzvphPVadHmGSYXB160EKEqe2UVdtqlfkKzABaXFYq4kvO6lZF9ggqDFjRuQnXw7499U6Ks3Y5pEJasC1mCxQwRn6pzxdXilFUy35ZLQuEGiRJdP8ZordgCKp6kAKttu2hVUeXpaNO_RgFnbZ_BX5PVY_Ix5IPTYTYbcb-AaIx5mAMQNOk4oSVDwTYdUv3BTinLt5rtuKNS9GliCMuFU6DCJF2xMK9kuTChZjV6gLVbXpD7yC0fID98RgFNUTAgPvvkYD12g6OqNSelEARkWXHby2cYjiOcdvbYwETFuujg74LOP11zhdKuGRQtf-81NaIRUInCLOulMY9vVOaaFI7gfz-PHfn8yPWzcyWWtlJn6rVysgX8jNaSpRKB6gkxVCaeKyjEW1TgJlVkVJrdnVEsqvrENFbknyEcUVg2WnfDztK-NDLaf00tBImdAJyhCqmGAFzkuw-cwMI-shty-PUFpsUkUhPZ9bPgNeW0o5cfuKdv9INuko7GaBmDrlcpeVzdOztJQkYSx6XHWWaydz3oRsVIYe_tfabbF6xfVDdOzwzZqR4xlq-wPBnjclLZHXJ3T0SW2bli0)

### 組件詳細架構
![組件詳細架構](https://www.plantuml.com/plantuml/svg/fLH1Rjim4Bpx5KHETclb0O9X97MBH43GkdPo3cXfBH2Hk0P9fJ2Qd8gUUkalqW_qIMtHNvGKHMWZEMg0w69wPyYT7UxoH1kkJ5KMenrFRtWExEWSjM6_lZxy_VBZpy_lln--7R7F8sQ_jE1QCmSt0VUzNa4kj57xDQXRaKBCacRLKiwhpTDeT6tXc3LXkoP8EDl3e6heVaJKjDkefZLMwh2KjP0tRJoSJr9gq5gWN22gJ4XkKBdatHZPa1O2f7adQvcBzizXhQ2epWiXGRz78xO2R9wmcYKEE2qAJBdXcmBc95-z9t0TkC2YWDHOWOFxhZGek--jgAbSxMV96o4pK5Fce7bFRxzifvqwrQHT6yKDvBRyv8oNS86RpNxT1RiD0sgSAwnsKyPRlPEMTINL48-85TAIjZo5eVJKTG_wO_KaPbtN-awii9BP2ZT23XJe-7uwNaKHxMSQ9urCA1uvt6NzC8kY-N-j7l9YTdTy_BoQxaVjkn26PU1j21JgzcaEqbYJwr_Q-jg016KHlsoSzrzqvYudPfMGfdyTVifUdkJQrWcyV7AQFEoYT3hQAgFeRjw8UUHKhMB09eyJ-KiXyvXj2-I6g5iY5h4dkoL1wfYLxOgQ-QBfg8TtvCCdsPtvo_qEfYoXZAQ-eELZ1MeVyDdbVEmcXFplNKUhy1at4fI70udwpWiSoglnPWWeBUSbB1KQJ9rBILWUw1OiTwY50LB48wLP5f0nTyaczAqgyHOwC14X6oI8fKPHfxn7RipfVT9uNwPjgHEGMLKM_m00)


## 時序圖

### 啟動與註冊流程
![啟動與註冊流程](https://www.plantuml.com/plantuml/svg/XLD1YnCn5BxdLpnwa8gwCDV2oCgoj4eHiNZpaksy7GDjCcGo8tjJ495TN3suWbNmu6Mzx45G3ug_nfdQV-5BqZHCHJqqJVg-xxtl-nxTBGtNffhF69yQfQ4tv7E42UfQJ16I7l0IXfE45OGIKr5mQQ1t5tDH6dq4oNtP7sfL5MbosK6fieKTgTAuahtyfOqTXpeqyAykT2reL1qu57qpbgQmUdpSFllKldlUdBnfpjut9sUhnMbxyHdIb36I3TUkEqa3aFWGmhn-lfPiFOkQ6_IwxH4PUnfjvKQE9E0IZ8cAadHfD9MMM-r3TMTYY3Rd1pFSbmRrVctPLrAufOJyBsMClCODnRsSpL_dtvgdnz5QKEeo7NY9EtjAIfoQVd2vZYoQz1km1r5Zq0COSPdD4DODX9AH4jV1DjTeuOvMjSWGRlgPgAs966ESC5Qva37sJQKB2v7VAwbog3GCBG6IhTcqV9kUnFAsCk9Gs0YhefWTzUsFqQy5ClfiE45b478L7C5Zkae253EMOQVXFtFmgtHF5uLKnD8YVs0O_565vQlpzlLtghGlNlxyyNPv-h6v-B9wz67vTR5SF04FPoWpi7rOKDBTIuRT_UuIdlxElm40)


### 客戶端連線與通訊流程
![客戶端連線與通訊流程](https://www.plantuml.com/plantuml/svg/jLGzRzim4DtrAuXCDj0Q44UX3b8OWPC2QLCRw9I5iIuE0ObKISg1jen5Xqv5XupjKg1eWWnjWFwVnic_A2bIeQJAXj6FXVhWUyTxzuwt8nKHAa-JWCmK5sZhC245Y2r49BAlGQO1T4OpmXJQ6fCKq2YXm9J1kDScvkcysjLldPk7d2SbmBmqz_Uuj5pzqg9EGIpe3FRounAWd-rzAIbd8yv5J1bHtCgeVJVn4vXJgKI1mXAdFyCL05m1S4SrN5ekodWQhi5WXy51C1oYdvSVRzzyNtwvkZ__jBovkBzuTtlv3Gs7GM1BZnvlkm97YC6PBMVleO9zunbT40ML3VPXQ6O_GYC1UjV3odARfGr1b2aGZ2JECW4g5ymzEjAniC68CmOprTDME8LzduF5HAW3v3Eon-dLfxqkUBSItIh-78yfEyWi1gEZH6YFi3YfgbKHA6UUOvegq8kcr1ldeKwknpoX1YezlRkvMhw_lht-iVpweMPe3TOEhMCcEWtqq6svf6oEfahe-FFYYFKcF6VnX1zJrcz7Ahisqklh7AGo920fweq5ppCK-eBPxZQB8UacZHoM72UmQypVNPZcakLFpolxUJprB6cj63pZb5LeroUrWg9TgNHaAayXTgUsLRjKmo26Pf8T2l4gXRYf40TciRdYpHLYhT2p_amXNgaGjnJYbiAgEGy5Nz0OQcT8psmrcYPKryKJzprpmgKMlFRYls0UFispB2cSsB3VFIBi9JtXmbktOKIyQKJiS7vub0yvSE68-GeVeVAfeyRGLEW7YQDFjMsPBbsQSKaLFEUAdb0GHymSgicdTl2Ta1aWLMGGr48QLngVNAhsYVxRgIbdmFtvnTtdjsl7-HZK0QXJ7eUzTMFSoNYPWoYgv9_iMwRfwt2xjtgOElB4jQulGbUSYbRBSRyRsQ6vIjXpoHcX2Z4NmFbdBs3_Y0tio3iWhEYbQBYDqe4ko2kIvA3hqVVnxCofxQ459Rx_sbEgaRwxidx0E9pTBw7MbwY1SVorfk6_CWt_8zFmngRX_smQRfk6lMcuQHh-VV1sc8QRfk4kqt27QJl0uZnDVW40)  

### 斷線處理流程
![斷線處理流程](https://www.plantuml.com/plantuml/svg/ZPAnJiCm48PtFuNLgGmKTOqKL5HGErG2jUe3kCbHMNBio7OYzGqG3y30o8I411iTU1vQU0jokM1XwC1GzvBxxlp_TnavBwol2iNAvbzciXJQyzv45C37IeBsFIvS5yRCsVexYz6Xv9KngWKmYFDJuwWMWpWrXxLqtcxui0MQn-41SGkmjSoWWoQB8MDfoj-V7tOth_kdbztTTh-zvsdA66ddnwUaC-7dqfN60J_1A3DQG-QPoBoiXOVEJ7DI3KflIyHAGQ384SCJ8JIH7Ef6zl103AqapIpnyeMt88e0aaqy6X3j91s1ryv0r71HW_Pzrxuy2dM8ikONgXpDPT3M13o7g227-DuSOi716Bc_r6Fo9OrUQbULfTg4rZ4gS6w3Rbpztzav5AO6VcrDGNDsF3DAKMVsKmRouJbi1LSSFe-FnxVFsoskcERs_pqGjjIsRGMMpwJkL4shLOG5pKi7bX3yyi3-cTa8T5uLwWi0)

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

Host 預設使用 `RoundRobinSelector` 進行路由，您可以實作 `ISessionSelectionStrategy` 介面來自訂路由邏輯：

```csharp
public interface ISessionSelectionStrategy
{
    IEnumerable<Registrys.ILineAllocatable> OrderAllocators(uint group, IReadOnlyList<Registrys.ILineAllocatable> allocators);
}
```

範例：

```csharp
public class CustomStrategy : ISessionSelectionStrategy
{
    public IEnumerable<Registrys.ILineAllocatable> OrderAllocators(uint group, IReadOnlyList<Registrys.ILineAllocatable> allocators)
    {
        // 自訂選擇邏輯，例如：
        // - 基於負載
        // - 基於地理位置
        // - 基於玩家偏好
        return allocators.OrderBy(a => a.AllocatedCount);
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
// 服務類型 A (Group 1)
var registryA = new Registry(1);

// 服務類型 B (Group 2)
var registryB = new Registry(2);

// 客戶端連接後，會同時與類型 A 服務和類型 B 服務建立連線
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
