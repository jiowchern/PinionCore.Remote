# 快速入門指南：Gateway Router Console Application

**日期**: 2025-10-23
**目的**: 提供本地開發、編譯、執行與 Docker 部署的快速啟動步驟

---

## 目錄

1. [環境需求](#環境需求)
2. [本地開發設置](#本地開發設置)
3. [編譯應用程式](#編譯應用程式)
4. [執行步驟](#執行步驟)
5. [基本測試流程](#基本測試流程)
6. [Docker 部署](#docker-部署)
7. [常見問題排解](#常見問題排解)

---

## 環境需求

### 開發環境

- **.NET SDK 8.0** 或更高版本
  - 下載: https://dotnet.microsoft.com/download/dotnet/8.0
  - 驗證: `dotnet --version`

- **作業系統**:
  - Windows 10/11 或 Windows Server 2019+
  - Linux (Ubuntu 20.04+, Debian 11+, etc.)

- **IDE** (可選但建議):
  - Visual Studio 2022 (17.8+)
  - Visual Studio Code + C# Extension
  - JetBrains Rider

### Docker 部署環境 (可選)

- **Docker Engine** 20.10+ 或 Docker Desktop
  - 下載: https://www.docker.com/get-started
  - 驗證: `docker --version`

- **Docker Compose** v2+
  - 驗證: `docker compose version`

---

## 本地開發設置

### 1. 複製儲存庫

```bash
# 複製 PinionCore.Remote 儲存庫
git clone https://github.com/jiowchern/PinionCore.Remote.git D:\develop\PinionCore.Remote
cd D:\develop\PinionCore.Remote

# 切換到功能分支
git checkout 002-gateway-router-console
```

### 2. 還原 NuGet 套件

```bash
# 在儲存庫根目錄執行
dotnet restore PinionCore.sln
```

### 3. 驗證專案結構

確認以下專案存在:
```
D:\develop\PinionCore.Remote\
├── PinionCore.Consoles.Gateway.Router\       # Router Console (將在任務執行時建立)
├── PinionCore.Consoles.Chat1.Server\         # Chat Server (現有專案)
├── PinionCore.Consoles.Chat1.Client\         # Chat Client (現有專案)
├── PinionCore.Consoles.Chat1.Bot\            # Bot 測試工具 (參考 Rx 用法)
├── PinionCore.Consoles.Chat1.Common\         # 共用 Protocol
├── PinionCore.Consoles.Chat1\                # 共用業務邏輯
└── PinionCore.sln                            # 方案檔案
```

---

## 編譯應用程式

### 1. 編譯所有專案

```bash
# 在儲存庫根目錄執行
dotnet build PinionCore.sln --configuration Release
```

### 2. 編譯個別專案 (可選)

```bash
# 編譯 Router Console
dotnet build PinionCore.Consoles.Gateway.Router\PinionCore.Consoles.Gateway.Router.csproj --configuration Release

# 編譯 Chat Server
dotnet build PinionCore.Consoles.Chat1.Server\PinionCore.Consoles.Chat1.Server.csproj --configuration Release

# 編譯 Chat Client
dotnet build PinionCore.Consoles.Chat1.Client\PinionCore.Consoles.Chat1.Client.csproj --configuration Release

# 編譯 Bot (測試工具)
dotnet build PinionCore.Consoles.Chat1.Bot\PinionCore.Consoles.Chat1.Bot.csproj --configuration Release
```

### 3. 驗證編譯結果

編譯成功後,執行檔位於各專案的 `bin\Release\net8.0\` 目錄:
```
PinionCore.Consoles.Gateway.Router\bin\Release\net8.0\PinionCore.Consoles.Gateway.Router.exe
PinionCore.Consoles.Chat1.Server\bin\Release\net8.0\PinionCore.Consoles.Chat1.Server.exe
PinionCore.Consoles.Chat1.Client\bin\Release\net8.0\PinionCore.Consoles.Chat1.Client.exe
PinionCore.Consoles.Chat1.Bot\bin\Release\net8.0\PinionCore.Consoles.Chat1.Bot.exe
```

---

## 執行步驟

### 場景 1: 最小完整系統 (1 Router + 1 Chat Server + Bot 測試)

#### 步驟 1: 啟動 Router Console

開啟第一個終端機:
```bash
cd D:\develop\PinionCore.Remote\PinionCore.Consoles.Gateway.Router\bin\Release\net8.0
.\PinionCore.Consoles.Gateway.Router.exe
```

**預期輸出**:
```
2025-10-23 14:30:15.123 [INFO] [RouterService] Router 啟動成功,負載平衡策略: Round-Robin
2025-10-23 14:30:15.456 [INFO] [AgentListenerService] Agent TCP 監聽已啟動,端口: 8001
2025-10-23 14:30:15.789 [INFO] [AgentListenerService] Agent WebSocket 監聽已啟動,端口: 8002
2025-10-23 14:30:16.012 [INFO] [RegistryListenerService] Registry TCP 監聽已啟動,端口: 8003
```

#### 步驟 2: 啟動 Chat Server (最大相容性模式)

開啟第二個終端機:
```bash
cd D:\develop\PinionCore.Remote\PinionCore.Consoles.Chat1.Server\bin\Release\net8.0
.\PinionCore.Consoles.Chat1.Server.exe --tcp-port=23916 --web-port=23917 --router-host=127.0.0.1 --router-port=8003 --group=1
```

**預期輸出**:
```
2025-10-23 14:31:00.123 [INFO] [CompositeConnectionService] TCP 直連模式已啟用,端口: 23916
2025-10-23 14:31:00.234 [INFO] [CompositeConnectionService] WebSocket 直連模式已啟用,端口: 23917
2025-10-23 14:31:00.345 [INFO] [RegistryClientService] 嘗試連接到 Router (127.0.0.1:8003)
2025-10-23 14:31:00.456 [INFO] [RegistryConnectionManager] 成功連接到 Router
2025-10-23 14:31:00.567 [INFO] [CompositeConnectionService] Gateway 路由模式已啟用,Router: 127.0.0.1:8003
2025-10-23 14:31:00.678 [INFO] [Program] Chat Server 啟動完成,模式: 最大相容性 (3 種連線來源)
```

**Router Console 應顯示**:
```
2025-10-23 14:31:00.500 [INFO] [RegistryListenerService] 新 Registry 連接,Group: 1, Version: [1, 0, 0]
```

#### 步驟 3: 使用 Bot 測試連線 (Router 模式)

開啟第三個終端機:
```bash
cd D:\develop\PinionCore.Remote\PinionCore.Consoles.Chat1.Bot\bin\Release\net8.0
.\PinionCore.Consoles.Chat1.Bot.exe --router-host=127.0.0.1 --router-port=8001 --name=TestBot
```

**預期輸出**:
```
2025-10-23 14:32:00.123 [INFO] Bot 連接到 Router (127.0.0.1:8001)
2025-10-23 14:32:00.234 [INFO] 等待路由分配...
2025-10-23 14:32:00.345 [INFO] 路由分配成功,已連接到 Chat Server
2025-10-23 14:32:00.456 [INFO] 登入成功,名稱: TestBot
2025-10-23 14:32:00.567 [INFO] 進入房間: Lobby
```

**Router Console 應顯示**:
```
2025-10-23 14:32:00.250 [INFO] [AgentListenerService] 新 Agent 連接 (TCP),Worker ID: agent-001
2025-10-23 14:32:00.360 [INFO] [RouterService] Agent 路由成功,Group: 1, Registry: registry-001
```

---

### 場景 2: 直連模式測試 (Bot 直連到 Chat Server)

#### 步驟 1: 啟動 Chat Server (僅直連模式)

```bash
.\PinionCore.Consoles.Chat1.Server.exe --tcp-port=23916
```

**預期輸出**:
```
2025-10-23 14:40:00.123 [INFO] [CompositeConnectionService] TCP 直連模式已啟用,端口: 23916
2025-10-23 14:40:00.234 [INFO] [Program] Chat Server 啟動完成,模式: 直連
```

#### 步驟 2: 使用 Bot 測試 (直連模式)

```bash
.\PinionCore.Consoles.Chat1.Bot.exe --server-host=127.0.0.1 --server-port=23916 --name=DirectBot
```

**預期輸出**:
```
2025-10-23 14:40:05.123 [INFO] Bot 直連到 Chat Server (127.0.0.1:23916)
2025-10-23 14:40:05.234 [INFO] 連接成功
2025-10-23 14:40:05.345 [INFO] 登入成功,名稱: DirectBot
2025-10-23 14:40:05.456 [INFO] 進入房間: Lobby
```

---

## 基本測試流程

### 1. 自動化測試：使用 Bot 驗證部署

**目標**: 驗證 Router 與 Chat Server 已正確部署並可正常路由

**參考**: `PinionCore.Consoles.Chat1.Bot` 的 Rx 調用模式

#### 測試腳本範例 (基於 Bot 的 Rx 模式)

**位置**: `tests/Integration.Tests/RouterDeploymentTests.cs`

```csharp
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Xunit;
using PinionCore.Network;
using PinionCore.Remote;

public class RouterDeploymentTests
{
    [Fact]
    public async Task TestRouterConnection_ShouldSucceed()
    {
        // Arrange
        var protocol = ProtocolCreator.Create();
        var agent = PinionCore.Remote.Standalone.Provider.CreateAgent(protocol);

        var connector = new PinionCore.Network.Tcp.Connector();
        var routerEndpoint = new System.Net.IPEndPoint(
            System.Net.IPAddress.Parse("127.0.0.1"),
            8001
        );

        // Act
        var peer = await connector.Connect(routerEndpoint);
        agent.Enable(peer);

        var agentWorker = new AgentWorker(agent);

        // 使用 Rx 訂閱登入流程 (參考 Chat1.Bot 模式)
        var loginSuccess = false;
        var obs = from entry in agent.QueryNotifier<IEntry>().SupplyEvent()
                  from loginResult in entry.Login("TestBot").RemoteValue()
                  select loginResult;

        obs.Subscribe(result => {
            loginSuccess = result;
        });

        // 等待結果
        await Task.Delay(2000);

        // Assert
        Assert.True(agent.Ping, "Agent 應保持連線");
        Assert.True(loginSuccess, "登入應成功");

        // Cleanup
        agentWorker.Dispose();
    }

    [Fact]
    public async Task TestChatServerDirectConnection_ShouldSucceed()
    {
        // Arrange
        var protocol = ProtocolCreator.Create();
        var agent = PinionCore.Remote.Standalone.Provider.CreateAgent(protocol);

        var connector = new PinionCore.Network.Tcp.Connector();
        var serverEndpoint = new System.Net.IPEndPoint(
            System.Net.IPAddress.Parse("127.0.0.1"),
            23916
        );

        // Act
        var peer = await connector.Connect(serverEndpoint);
        agent.Enable(peer);

        var agentWorker = new AgentWorker(agent);

        // 使用 Rx 訂閱登入流程
        var loginSuccess = false;
        var obs = from entry in agent.QueryNotifier<IEntry>().SupplyEvent()
                  from loginResult in entry.Login("DirectTestBot").RemoteValue()
                  select loginResult;

        obs.Subscribe(result => {
            loginSuccess = result;
        });

        // 等待結果
        await Task.Delay(2000);

        // Assert
        Assert.True(agent.Ping, "Agent 應保持連線");
        Assert.True(loginSuccess, "直連登入應成功");

        // Cleanup
        agentWorker.Dispose();
    }

    [Fact]
    public async Task TestMaxCompatibilityMode_AllSourcesShouldWork()
    {
        // Arrange
        var protocol = ProtocolCreator.Create();

        // 建立三個 Agent (Router、直連 TCP、直連 WebSocket)
        var routerAgent = CreateAgent(protocol, "127.0.0.1", 8001);      // 透過 Router
        var tcpAgent = CreateAgent(protocol, "127.0.0.1", 23916);        // 直連 TCP
        var webAgent = CreateWebAgent(protocol, "ws://127.0.0.1:23917"); // 直連 WebSocket

        // Act & Assert
        await Task.Delay(3000);

        Assert.True(routerAgent.Ping, "Router 模式連線應成功");
        Assert.True(tcpAgent.Ping, "TCP 直連應成功");
        Assert.True(webAgent.Ping, "WebSocket 直連應成功");
    }
}
```

#### 執行測試

```bash
# 確保 Router 與 Chat Server 已啟動
dotnet test tests/Integration.Tests/Integration.Tests.csproj --filter "FullyQualifiedName~RouterDeploymentTests"
```

**成功標準**:
- 所有測試通過 (綠色 ✓)
- 無連線超時或異常

---

### 2. 手動測試：使用 Bot 發送訊息

**目標**: 驗證透過 Router 的訊息轉發功能正常

**步驟**:
1. 啟動 Router 與 Chat Server
2. 啟動 Bot 1:
   ```bash
   .\PinionCore.Consoles.Chat1.Bot.exe --router-host=127.0.0.1 --router-port=8001 --name=Alice
   ```
3. 啟動 Bot 2:
   ```bash
   .\PinionCore.Consoles.Chat1.Bot.exe --router-host=127.0.0.1 --router-port=8001 --name=Bob
   ```
4. 兩個 Bot 自動進入 Lobby 房間
5. 觀察 Chat Server 日誌顯示兩個使用者的互動

**成功標準**:
- 兩個 Bot 都能登入並進入房間
- Chat Server 日誌顯示訊息交互
- 無錯誤或異常日誌

---

### 3. 混合連線測試 (最大相容性模式)

**目標**: 驗證 Chat Server 同時處理三種連線來源

**步驟**:
1. 啟動 Router 與 Chat Server (最大相容性模式)
2. 啟動 Bot 1 (Router 模式):
   ```bash
   .\PinionCore.Consoles.Chat1.Bot.exe --router-host=127.0.0.1 --router-port=8001 --name=RouterBot
   ```
3. 啟動 Bot 2 (直連 TCP):
   ```bash
   .\PinionCore.Consoles.Chat1.Bot.exe --server-host=127.0.0.1 --server-port=23916 --name=TcpBot
   ```
4. 啟動 Bot 3 (直連 WebSocket):
   ```bash
   .\PinionCore.Consoles.Chat1.Bot.exe --server-ws=ws://127.0.0.1:23917 --name=WebBot
   ```
5. 觀察三個 Bot 的日誌與 Chat Server 日誌

**成功標準**:
- 三個 Bot 都能登入並進入房間
- Chat Server 日誌顯示「3 種連線來源」
- 所有 Bot 能正常互動

---

### 4. 優雅關閉測試

**目標**: 驗證 SIGTERM/SIGINT 處理正確

**步驟 (Windows)**:
1. 在 Router Console 終端機按 `Ctrl+C`
2. 觀察日誌輸出

**預期輸出**:
```
2025-10-23 14:45:00.000 [INFO] [Program] 收到 SIGINT 訊號,開始優雅關閉...
2025-10-23 14:45:00.100 [INFO] [GracefulShutdownHandler] 關閉監聽器...
2025-10-23 14:45:00.200 [INFO] [GracefulShutdownHandler] 關閉 2 個 Agent 連線...
2025-10-23 14:45:02.500 [INFO] [GracefulShutdownHandler] 關閉 Router 服務...
2025-10-23 14:45:02.600 [INFO] [GracefulShutdownHandler] 寫入日誌檔案...
2025-10-23 14:45:02.700 [INFO] [Program] 優雅關閉完成
```

**步驟 (Linux/Docker)**:
```bash
# 發送 SIGTERM
kill -TERM <pid>

# 或使用 docker stop (自動發送 SIGTERM)
docker stop router-console
```

**成功標準**:
- 所有日誌正確寫入檔案
- 20 秒內完成關閉
- 無錯誤訊息或異常

---

## Docker 部署

### 1. 建立 Docker 映像檔

#### 建立 Router Console 映像

```bash
cd D:\develop\PinionCore.Remote
docker build -f docker/Dockerfile.router -t pinioncore-router:latest .
```

#### 建立 Chat Server 映像

```bash
docker build -f docker/Dockerfile.chatserver -t pinioncore-chatserver:latest .
```

### 2. 使用 Docker Compose 啟動完整環境

#### 編輯 docker-compose.yml

範例配置 (1 Router + 2 Chat Servers):
```yaml
version: '3.8'

services:
  router:
    image: pinioncore-router:latest
    container_name: gateway-router
    ports:
      - "8001:8001"  # Agent TCP (對外映射)
      - "8002:8002"  # Agent WebSocket (對外映射)
    environment:
      - AGENT_TCP_PORT=8001
      - AGENT_WEB_PORT=8002
      - REGISTRY_TCP_PORT=8003
    networks:
      - pinioncore-net
    restart: unless-stopped

  chatserver1:
    image: pinioncore-chatserver:latest
    container_name: chat-server-1
    depends_on:
      - router
    environment:
      - ROUTER_HOST=gateway-router
      - ROUTER_PORT=8003
      - GROUP=1
      - TCP_PORT=23916
      - WEB_PORT=23917
    networks:
      - pinioncore-net
    restart: unless-stopped

  chatserver2:
    image: pinioncore-chatserver:latest
    container_name: chat-server-2
    depends_on:
      - router
    environment:
      - ROUTER_HOST=gateway-router
      - ROUTER_PORT=8003
      - GROUP=1
      - TCP_PORT=23916
      - WEB_PORT=23917
    networks:
      - pinioncore-net
    restart: unless-stopped

networks:
  pinioncore-net:
    driver: bridge
```

#### 啟動環境

```bash
cd D:\develop\PinionCore.Remote\docker
docker compose up -d
```

**預期輸出**:
```
[+] Running 4/4
 ✔ Network docker_pinioncore-net  Created
 ✔ Container gateway-router       Started
 ✔ Container chat-server-1        Started
 ✔ Container chat-server-2        Started
```

### 3. 查看日誌

```bash
# 查看所有容器日誌
docker compose logs -f

# 查看特定容器日誌
docker compose logs -f router
docker compose logs -f chatserver1
```

### 4. 從本地使用 Bot 測試

```bash
cd D:\develop\PinionCore.Remote\PinionCore.Consoles.Chat1.Bot\bin\Release\net8.0
.\PinionCore.Consoles.Chat1.Bot.exe --router-host=127.0.0.1 --router-port=8001 --name=DockerTestBot
```

**預期行為**:
- Bot 連接到 Docker 容器中的 Router (透過端口映射 8001)
- Router 將 Bot 路由到 chat-server-1 或 chat-server-2 (Round-Robin)
- 登入成功並進入 Lobby 房間

### 5. 停止環境

```bash
docker compose down
```

**清理容器與網路**:
```bash
docker compose down -v
```

---

## 常見問題排解

### Q1: Router 啟動失敗,顯示「端口已被占用」

**錯誤訊息**:
```
2025-10-23 14:30:15.123 [ERROR] [AgentListenerService] Agent TCP 監聽器啟動失敗: 端口 8001 已被占用
```

**解決方法**:

1. **檢查端口占用** (Windows):
   ```cmd
   netstat -ano | findstr :8001
   ```
   找到 PID 後:
   ```cmd
   taskkill /PID <pid> /F
   ```

2. **檢查端口占用** (Linux):
   ```bash
   sudo lsof -i :8001
   sudo kill -9 <pid>
   ```

3. **使用不同端口**:
   ```bash
   .\PinionCore.Consoles.Gateway.Router.exe --agent-tcp-port=9001 --agent-web-port=9002 --registry-tcp-port=9003
   ```

---

### Q2: Chat Server 無法連接到 Router

**錯誤訊息**:
```
2025-10-23 14:31:00.345 [ERROR] [RegistryClientService] 連接到 Router 失敗: Connection refused
```

**排查步驟**:

1. **確認 Router 已啟動**:
   - 檢查 Router Console 是否顯示「Registry TCP 監聽已啟動,端口: 8003」

2. **檢查網路連通性**:
   ```bash
   # Windows
   Test-NetConnection 127.0.0.1 -Port 8003

   # Linux
   telnet 127.0.0.1 8003
   ```

3. **檢查防火牆規則**:
   - Windows Defender Firewall: 允許應用程式或端口
   - Linux iptables: 確認入站規則

4. **Docker 環境**: 確認容器間使用 Docker 網路名稱而非 localhost
   ```yaml
   environment:
     - ROUTER_HOST=gateway-router  # 使用容器名稱
     - ROUTER_PORT=8003
   ```

---

### Q3: Bot 連接後一直等待路由分配

**症狀**:
```
2025-10-23 14:32:00.234 [INFO] 等待路由分配...
(長時間無回應)
```

**可能原因**:

1. **無可用的 Registry**:
   - 檢查 Chat Server 是否成功註冊到 Router
   - 查看 Router 日誌是否有「新 Registry 連接」訊息

2. **協議版本不匹配**:
   - Chat Server 與 Bot 使用的 Protocol 版本不同
   - 重新編譯所有專案確保版本一致

3. **Group ID 不匹配** (如果未來支援多 Group):
   - 確認 Chat Server 與 Bot 使用相同的 Group

**解決方法**:
- 重新啟動 Chat Server,觀察註冊日誌
- 確認所有專案編譯自同一個程式碼版本

---

### Q4: Docker 容器無法訪問日誌檔案

**症狀**: 日誌只輸出到 stdout,無法找到日誌檔案

**原因**: Docker 容器內檔案系統隔離

**解決方法**:

1. **使用 docker logs 查看日誌**:
   ```bash
   docker logs gateway-router
   docker logs chat-server-1
   ```

2. **使用 Volume 映射日誌目錄**:
   ```yaml
   services:
     router:
       image: pinioncore-router:latest
       volumes:
         - ./logs:/app/logs  # 映射到主機目錄
   ```

3. **集中式日誌系統** (生產環境建議):
   - 使用 Docker Logging Driver (如 json-file, syslog)
   - 整合 ELK Stack (Elasticsearch + Logstash + Kibana)

---

## 效能基準測試 (可選)

### 1. 並發連線測試：使用多個 Bot

**目標**: 驗證 Router 支援 50 個並發 Agent 連線

**測試腳本**:
```bash
# 啟動 50 個 Bot 實例
for i in {1..50}
do
  .\PinionCore.Consoles.Chat1.Bot.exe --router-host=127.0.0.1 --router-port=8001 --name="Bot$i" &
done
```

**觀察項目**:
- Router 日誌顯示 50 個「新 Agent 連接」
- Router CPU 使用率 <80%
- 記憶體使用 <200MB

**成功標準**:
- 所有 Bot 成功路由
- 無連線超時或錯誤

---

### 2. 訊息延遲測試：使用 Bot 互相發送訊息

**工具**: 修改 Bot 程式碼,記錄發送與接收時間戳

**成功標準**:
- 平均延遲 <10ms
- 99% 延遲 <20ms

---

## 下一步

完成快速入門後,建議:

1. **閱讀架構文件**: `ARCHITECTURE.md` (將在文件任務中建立)
2. **查看 Bot 範例程式碼**: `PinionCore.Consoles.Chat1.Bot\Program.cs` (學習 Rx 調用模式)
3. **查看 Gateway 測試**: `PinionCore.Remote.Gateway.Test\Tests.cs`
4. **執行整合測試**: `dotnet test tests/Integration.Tests`
5. **探索進階配置**: 自訂負載平衡策略、多 Group 部署

---

## 參考資源

- **PinionCore.Remote 儲存庫**: https://github.com/jiowchern/PinionCore.Remote
- **Gateway 套件測試**: `PinionCore.Remote.Gateway.Test\Tests.cs`
- **Chat1 Bot 範例**: `PinionCore.Consoles.Chat1.Bot\Program.cs` (Rx 調用模式參考)
- **Chat1 Server**: `PinionCore.Consoles.Chat1.Server\Program.cs`
- **Docker 官方文件**: https://docs.docker.com/

---

**快速入門指南完成**
**版本**: 1.0
**最後更新**: 2025-10-23
