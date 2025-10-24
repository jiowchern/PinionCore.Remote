# 技術研究報告：Gateway Router Console Application

**日期**: 2025-10-23
**階段**: Phase 0 - Research
**目的**: 探索 PinionCore.Remote.Gateway 與 PinionCore.Network 整合模式,確立實作策略

---

## 研究概述

本文件記錄 Gateway Router Console Application 開發所需的關鍵技術決策與最佳實踐。研究涵蓋五個主題:Gateway-Network 整合、ILineAllocatable 實作、Registry 重連策略、最大相容性連線架構、優雅關閉模式。

所有研究基於 PinionCore.Remote 專案現有程式碼與測試範例,確保方案與框架設計理念一致。

---

## 1. PinionCore.Remote.Gateway 與 PinionCore.Network 整合模式

### 1.1 Router 建立與初始化

**核心類別**: `PinionCore.Remote.Gateway.Router` (D:\develop\PinionCore.Remote\PinionCore.Remote.Gateway\Router.cs)

**建構子簽章**:
```csharp
public Router(ISessionSelectionStrategy strategy)
```

**初始化模式**:
```csharp
// 使用 Round-Robin 負載平衡策略
using var router = new PinionCore.Remote.Gateway.Router(
    new PinionCore.Remote.Gateway.Hosts.RoundRobinSelector()
);

// Router 提供兩個服務端點
IService registryEndpoint = router.Registry;  // Registry Client 連接端點
IService sessionEndpoint = router.Session;    // Agent 連接端點
```

**關鍵設計**:
- Router 需要 `ISessionSelectionStrategy` 決定如何分配 Agent 到 Registry
- 提供雙端點架構:Registry(給遊戲服務) 與 Session(給客戶端)
- 內部使用 `SessionHub` 管理路由邏輯,`Registrys.Server` 管理 Registry 註冊

**決策**: Router Console 應用程式將使用 `RoundRobinSelector` 作為預設策略,符合規格需求(FR-016)。

---

### 1.2 Network Listener 建立與綁定

#### TCP Listener

**類別**: `PinionCore.Network.Tcp.Listener` (D:\develop\PinionCore.Remote\PinionCore.Network\Tcp\Listener.cs)

**使用模式**:
```csharp
var listener = new PinionCore.Network.Tcp.Listener();

// 綁定端口並開始監聽
listener.Bind(port: 8001, backlog: 100);

// 訂閱連線事件
listener.AcceptEvent += (peer) =>
{
    // peer 是 IStreamable 實例,代表一個新的 TCP 連線
    HandleNewConnection(peer);
};
```

**特性**:
- 自動設定 `Socket.NoDelay = true` 降低延遲
- 使用非同步 BeginAccept/EndAccept 模式
- 每個接受的連線產生 `Peer` 物件(實現 `IStreamable`)

#### WebSocket Listener

**類別**: `PinionCore.Network.Web.Listener` (D:\develop\PinionCore.Remote\PinionCore.Network\Web\Listener.cs)

**使用模式**:
```csharp
var webListener = new PinionCore.Network.Web.Listener();

// 綁定 URL (需包含 trailing slash)
webListener.Bind("http://0.0.0.0:8002/");

// 訂閱連線事件
webListener.AcceptEvent += (peer) =>
{
    // peer 是 IStreamable 實例,底層使用 WebSocket
    HandleNewConnection(peer);
};
```

**特性**:
- 基於 `System.Net.HttpListener` 自動處理 WebSocket 握手
- 使用 `AcceptWebSocketAsync()` 升級連線
- 產生的 `Peer` 包裝 `System.Net.WebSockets.WebSocket`

**決策**: Router Console 需要為 Agent 端點建立兩個監聽器(TCP + WebSocket),為 Registry 端點建立一個 TCP 監聽器。

---

