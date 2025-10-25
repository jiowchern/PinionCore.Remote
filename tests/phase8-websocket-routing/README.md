# Phase 8: WebSocket Agent Routing Test Guide

## 測試目標
驗證 Gateway Client 使用 WebSocket 協議連接到 Router：
- Client 透過 WebSocket 連接到 Router Agent 端點 (8002)
- Router 自動路由 WebSocket Client 到 Chat Server
- WebSocket Client 與 TCP Client 能互相通訊

## 測試架構

```
TCP Client ───────┐
                  │
                  ├──→ Router (8001 TCP, 8002 WS) ──→ Chat Server (Registry 8003)
                  │
WebSocket Client ─┘
```

## 測試步驟

### 步驟 1: 編譯專案

```cmd
cd D:\develop\PinionCore.Remote
dotnet build --configuration Debug
```

### 步驟 2: 啟動 Router

**終端 1**:
```cmd
cd D:\develop\PinionCore.Remote\PinionCore.Consoles.Gateway.Router\bin\Debug\net8.0
PinionCore.Consoles.Gateway.Router.exe
```

**預期輸出**:
```
[Info]Router 配置: Agent TCP=8001, Agent WebSocket=8002, Registry TCP=8003
[Info]Agent TCP 監聽已啟動，端口: 8001
[Info]Agent WebSocket 監聽已啟動，端口: 8002
[Info]Router Console 啟動完成，所有監聽器已就緒
```

### 步驟 3: 啟動 Chat Server (Registry 模式)

**終端 2**:
```cmd
cd D:\develop\PinionCore.Remote\PinionCore.Consoles.Chat1.Server\bin\Debug\net8.0
PinionCore.Consoles.Chat1.Server.exe --router-host=127.0.0.1 --router-port=8003 --group=1
```

**預期輸出**:
```
[Info]Registry 狀態: 連接中 (127.0.0.1:8003)
[Info]成功連接到 Router
[Info]Registry 狀態: 已連接
```

### 步驟 4A: 啟動 TCP Client

**終端 3**:
```cmd
cd D:\develop\PinionCore.Remote\PinionCore.Consoles.Chat1.Client\bin\Debug\net8.0
PinionCore.Consoles.Chat1.Client.exe --router-host=127.0.0.1 --router-port=8001
```

**預期輸出**:
```
Gateway Router mode (TCP).
Router: 127.0.0.1:8001
[TCP] Connecting to Router at 127.0.0.1:8001...
[TCP] Connected to Router. Waiting for routing allocation...
```

### 步驟 4B: 啟動 WebSocket Client

**終端 4**:
```cmd
D:\develop\PinionCore.Remote\tests\phase8-websocket-routing\start-client-websocket.cmd
```

或直接執行：
```cmd
cd D:\develop\PinionCore.Remote\PinionCore.Consoles.Chat1.Client\bin\Debug\net8.0
PinionCore.Consoles.Chat1.Client.exe --router-host=localhost --router-port=8002 --websocket
```

**預期輸出**:
```
Gateway Router mode (WebSocket).
Router: ws://localhost:8002/
[WebSocket] Connecting to Router at ws://localhost:8002/...
[WebSocket] Connected to Router. Waiting for routing allocation...
```

### 步驟 5: 聊天測試

**在 TCP Client (終端 3)**:
```
login alice password123
say Hello from TCP!
```

**在 WebSocket Client (終端 4)**:
```
login bob password456
say Hello from WebSocket!
```

**預期結果**:
- TCP Client 能看到 WebSocket Client 的訊息
- WebSocket Client 能看到 TCP Client 的訊息
- 兩個客戶端能正常聊天

## 驗收標準

### ✅ T062 - WebSocket 連接
- [ ] Client 能透過 WebSocket 連接到 Router (8002)
- [ ] 連接成功後顯示正確的協議標識

### ✅ T063 - WebSocket 路由
- [ ] Router 自動路由 WebSocket Client 到 Chat Server
- [ ] WebSocket Client 能正常登入

### ✅ T064 - 跨協議通訊
- [ ] WebSocket Client 能接收 TCP Client 的訊息
- [ ] TCP Client 能接收 WebSocket Client 的訊息

### ✅ T065 - 錯誤處理
- [ ] Router 未啟動時顯示清晰錯誤
- [ ] WebSocket 連接失敗時顯示清晰錯誤

## 比較測試：TCP vs WebSocket

| 項目 | TCP Mode | WebSocket Mode |
|------|----------|----------------|
| 端口 | 8001 | 8002 |
| 連接字串 | `127.0.0.1:8001` | `ws://127.0.0.1:8002/` |
| 參數 | `--router-host=127.0.0.1 --router-port=8001` | `--router-host=127.0.0.1 --router-port=8002 --websocket` |
| 顯示 | `[TCP] Connecting...` | `[WebSocket] Connecting...` |

## 故障排除

### 問題：WebSocket 連接失敗
```
錯誤: WebSocket 連接失敗 - ...
```

**可能原因**:
1. Router 未啟動
2. Router WebSocket 端口未正確啟動
3. 防火牆阻擋

**解決方式**:
1. 確認 Router 日誌顯示 "Agent WebSocket 監聽已啟動，端口: 8002"
2. 使用 `netstat -an | findstr "8002"` 檢查端口狀態
3. 檢查防火牆設置

### 問題：連接後無法登入
```
[WebSocket] Connected to Router. Waiting for routing allocation...
(but no login prompt)
```

**可能原因**:
- Chat Server 未連接到 Router
- Router 路由邏輯問題

**解決方式**:
1. 確認 Chat Server 顯示 "Registry 狀態: 已連接"
2. 檢查 Router 日誌是否顯示 Registry 連接
3. 重啟所有組件

## 技術實現

### WebSocket Connector API
```csharp
var clientWebSocket = new ClientWebSocket();
var webConnector = new PinionCore.Network.Web.Connecter(clientWebSocket);
var address = $"ws://{routerHost}:{routerPort}/";

bool connected = webConnector.ConnectAsync(address).GetAwaiter().GetResult();

if (connected)
{
    Agent.Enable(webConnector);
}
```

### Router WebSocket 監聽器
```csharp
var webListener = new Web.Listener();
webListener.Bind($"http://localhost:{webPort}/");
```

### 雙協議支援
- TCP: `PinionCore.Network.Tcp.Connector`
- WebSocket: `PinionCore.Network.Web.Connecter`
- 統一介面: `IStreamable`
