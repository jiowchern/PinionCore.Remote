# 實作計劃：Gateway Router Console Application

**分支**: `002-gateway-router-console` | **日期**: 2025-10-23 | **規格**: [spec.md](./spec.md)
**輸入**: 功能規格來自 `/specs/002-gateway-router-console/spec.md`

## 概要

本功能實現基於 PinionCore.Remote.Gateway 套件的路由服務主控台應用程式,包含三個主要元件:

1. **Router Console**: 核心路由服務,監聽 Agent(TCP/WebSocket) 與 Registry(TCP) 連接,執行智能路由分配
2. **Enhanced Chat Server**: 擴展現有 Chat1.Server,支援最大相容性連線模式(直接 TCP + 直接 WebSocket + Gateway 路由)
3. **Enhanced Chat Client**: 擴展現有 Chat1.Client,支援透過 Router 路由或直連到 Chat Server

技術路徑採用 PinionCore 原生框架實作,不依賴第三方通訊框架(如 gRPC),使用 PinionCore.Network 提供 TCP/WebSocket 連線能力,使用 PinionCore.Utility 提供原生日誌功能,並透過 Docker Compose 實現容器化部署。

## 技術背景

**語言/版本**: .NET 8.0
**主要依賴項**: PinionCore.Remote.Gateway, PinionCore.Network, PinionCore.Utility, Microsoft.Extensions.Configuration.CommandLine
**儲存**: N/A(無狀態路由服務)
**測試**: xUnit 或 NUnit(.NET 原生測試框架)
**目標平台**: Linux/Windows Server, Docker 容器
**專案類型**: 主控台應用程式(Console Application)
**效能目標**: 支援 50 個並發 Agent 連線、5 個並發 Registry 連線、路由延遲 <10ms
**約束條件**:
- 20 秒優雅關閉超時時間
- 等待匹配機制(無超時、無拒絕)
- 嚴禁使用 static class(網路通訊框架設計原則)
- 必須使用 PinionCore.Utility.Log + LogFileRecorder(不使用第三方日誌庫)
- 端口衝突時顯示錯誤並終止
- 最大相容性模式(Chat Server 同時支援三種連線來源)

**規模/範疇**:
- 3 個主控台應用程式(1 Router + 2 Enhanced Chat Applications)
- Docker 部署(Router + Chat Server,不含 Client)
- 55 個功能需求,15 個成功標準
- 支援多協議版本並存、負載平衡(Round-Robin)、Registry 自動重連

## 憲章檢查

*閘門:必須在第 0 階段研究前通過。在第 1 階段設計後重新檢查。*

目前專案憲章檔案(`.specify/memory/constitution.md`)尚未填入實際內容,為模板狀態。

基於 PinionCore.Remote 專案的 CLAUDE.md 指導原則:
- ✅ **不使用 static class**: 此為網路通訊框架的核心設計原則,Router 與 Enhanced Chat 應用程式將遵循此規則
- ✅ **使用原生套件**: 專案需求明確要求使用 PinionCore.Network、PinionCore.Remote.Gateway、PinionCore.Utility,嚴禁第三方通訊框架
- ✅ **Protocol 生成機制**: 若需定義新的通訊介面,將使用 `[PinionCore.Remote.Protocol.Creator]` 屬性觸發 Source Generator

**目前狀態:通過**(基於專案既有原則)

## 專案結構

### 文件(本功能)

```text
specs/002-gateway-router-console/
├── plan.md              # 本檔案(/speckit.plan 指令輸出)
├── research.md          # 第 0 階段輸出(/speckit.plan 指令)
├── data-model.md        # 第 1 階段輸出(/speckit.plan 指令)
├── quickstart.md        # 第 1 階段輸出(/speckit.plan 指令)
├── contracts/           # 第 1 階段輸出(/speckit.plan 指令)
├── tasks.md             # 第 2 階段輸出(/speckit.tasks 指令 - 不由 /speckit.plan 建立)
└── checklists/
    └── requirements.md  # 規格品質檢查清單(已完成)
```

### 原始碼(儲存庫根目錄)

```text
# Router Console 應用程式
PinionCore.Consoles.Gateway.Router/          # 新建專案
├── Program.cs                                # 應用程式進入點
├── RouterApplication.cs                      # 核心應用程式邏輯
├── Configuration/
│   └── CommandLineOptions.cs                # 命令列參數解析
├── Services/
│   ├── RouterService.cs                      # Router 服務包裝
│   ├── AgentListenerService.cs               # Agent 監聽服務(TCP + WebSocket)
│   └── RegistryListenerService.cs            # Registry 監聽服務(TCP)
├── Logging/
│   └── LoggingConfiguration.cs               # 日誌配置(stdout + 檔案)
└── PinionCore.Consoles.Gateway.Router.csproj # 專案檔案

# Enhanced Chat Server(擴展現有專案)
PinionCore.Consoles.Chat1.Server/             # 現有專案
├── Program.cs                                # [修改] 添加 Gateway 模式支援
├── Services/
│   ├── RegistryClientService.cs              # [新增] Registry Client 連接服務
│   └── CompositeConnectionService.cs         # [新增] 最大相容性連線管理
├── Configuration/
│   └── GatewayConfiguration.cs               # [新增] Gateway 模式配置
└── [其他現有檔案保留]

# Enhanced Chat Client(擴展現有專案)
PinionCore.Consoles.Chat1.Client/             # 現有專案
├── Program.cs                                # [修改] 添加 Router 模式支援
├── Services/
│   └── RouterConnectionService.cs            # [新增] Router 連接服務
├── Configuration/
│   └── RouterConfiguration.cs                # [新增] Router 模式配置
└── [其他現有檔案保留]

# Docker 部署
docker/
├── Dockerfile.router                         # Router Console Dockerfile
├── Dockerfile.chatserver                     # Enhanced Chat Server Dockerfile
├── docker-compose.yml                        # 容器編排配置(1 Router + 2 Chat Servers)
└── DOCKER.md                                 # Docker 部署文件

# 測試專案(待第 2 階段任務產生時決定)
tests/
├── PinionCore.Consoles.Gateway.Router.Tests/  # Router 單元測試
├── Integration.Tests/                         # 整合測試(Router + Chat Server + Chat Client)
└── Docker.Tests/                              # Docker 部署測試
```

**結構決策**:

1. **Router Console**: 新建獨立專案於 `D:\develop\PinionCore.Remote\PinionCore.Consoles.Gateway.Router`,加入 `PinionCore.sln` 的 `gateway` 方案資料夾
2. **Enhanced Chat Applications**: 基於現有 `Chat1.Server` 與 `Chat1.Client` 專案擴展,保留原有功能並添加 Gateway 模式
3. **模組化設計**: 每個服務功能獨立封裝(RouterService、AgentListenerService、RegistryListenerService、RegistryClientService),避免 static class
4. **Docker 檔案集中管理**: 所有 Docker 相關檔案統一放置於 `docker/` 目錄,便於部署管理
5. **測試分層**: 單元測試、整合測試、Docker 測試分離,確保各層級功能正確性

## 複雜度追蹤

> **僅在憲章檢查有違規需要說明時填寫**

| 違規項目 | 需要原因 | 拒絕更簡單替代方案的原因 |
|---------|---------|-------------------------|
| N/A     | N/A     | N/A                     |

**說明**:本功能設計符合 PinionCore.Remote 專案的既有架構原則,無違規項目。

---

## 第 0 階段:研究

**目標**:探索 Gateway-Network 整合模式、最佳實踐、實作策略,記錄於 `research.md`

**關鍵研究主題**:

1. **PinionCore.Remote.Gateway 與 PinionCore.Network 整合模式**
   - Router 如何使用 Tcp.Listener / Web.Listener 建立監聽器
   - Agent 與 Registry Client 如何使用 Tcp.Connector 連接到 Router
   - IStreamable 抽象如何在 Gateway 內部使用

2. **ILineAllocatable 實作模式**
   - Registry Client 必須實現的介面結構
   - Stream 分配與回收的生命週期管理
   - Chat Server 如何整合此介面

3. **Registry 重連策略**
   - 指數退避演算法設計
   - 重連失敗處理機制
   - 連線狀態管理最佳實踐

4. **最大相容性連線架構**
   - CompositeListenable 使用模式(參考 Chat1.Server 現有實作)
   - 如何統一處理來自三種來源的 IStreamable
   - 業務邏輯層如何無差別處理不同來源的連線

5. **優雅關閉模式**
   - .NET 中 SIGTERM/SIGINT 訊號捕捉
   - 20 秒超時內關閉所有連線與監聽器的策略
   - Log.Shutdown() 與 LogFileRecorder 的正確關閉順序

**產出**:`research.md` 文件,記錄技術決策、替代方案、最佳實踐與範例程式碼

---

## 第 1 階段:設計

**目標**:定義資料模型、合約介面、快速入門指南

**產出文件**:

1. **data-model.md**:定義核心實體與資料結構
   - Router 內部狀態管理(SessionCoordinator、Line、ISessionSelectionStrategy)
   - Registry Client 狀態(連線狀態、重連狀態機)
   - Agent 狀態(等待中、已路由、已斷線)
   - 日誌資料結構(事件類型、訊息格式)

2. **contracts/**:API 合約與介面定義
   - `IRouterService.cs`:Router 服務介面
   - `IRegistryClientService.cs`:Registry Client 服務介面
   - `IAgentConnectionService.cs`:Agent 連接服務介面
   - `ILoggingService.cs`:日誌服務介面
   - 命令列參數結構(RouterOptions, ChatServerOptions, ChatClientOptions)

3. **quickstart.md**:快速入門指南
   - 本地開發環境設置
   - 編譯與執行步驟(Router → Chat Server → Chat Client)
   - 基本測試流程(建立連線、發送訊息、優雅關閉)
   - Docker 部署快速啟動(docker-compose up)
   - 常見問題排解(端口衝突、連線失敗、日誌查看)

**設計原則**:
- 所有服務介面避免 static,支援依賴注入與單元測試
- 命令列參數使用強型別 Options 類別封裝
- 日誌訊息使用結構化格式(包含時間戳、級別、來源、訊息)

---

## 第 2 階段:任務產生

**注意**:此階段由 `/speckit.tasks` 指令執行,不在 `/speckit.plan` 範圍內。

執行 `/speckit.tasks` 將產生 `tasks.md`,包含依賴順序排列的可執行任務清單,涵蓋:
- Router Console 應用程式開發
- Enhanced Chat Server 擴展開發
- Enhanced Chat Client 擴展開發
- Docker 檔案與部署配置
- 測試專案建立與測試案例實作
- 文件撰寫與範例程式碼

---

## 附錄:關鍵技術細節摘要

### 日誌模式(來自規格 FR-014, FR-015)

```csharp
// 初始化(在 Program.cs 或主類別)
var log = PinionCore.Utility.Log.Instance;
var fileRecorder = new PinionCore.Utility.LogFileRecorder("RouterConsole");

// 配置 stdout 輸出
log.RecordEvent += System.Console.WriteLine;

// 配置檔案輸出
log.RecordEvent += fileRecorder.Record;

// 寫入日誌
log.WriteInfo("Router started successfully");
log.WriteInfo(() => $"Listening on port {port}");

// 優雅關閉
fileRecorder.Save();
fileRecorder.Close();
log.Shutdown();  // 等待非同步佇列清空
```

### 命令列參數範例

**Router Console**:
```bash
PinionCore.Consoles.Gateway.Router \
  --agent-tcp-port=8001 \
  --agent-web-port=8002 \
  --registry-tcp-port=8003
```

**Enhanced Chat Server(最大相容性模式)**:
```bash
PinionCore.Consoles.Chat1.Server \
  --tcp-port=23916 \
  --web-port=23917 \
  --router-host=127.0.0.1 \
  --router-port=8003 \
  --group=1
```

**Enhanced Chat Client(Router 模式)**:
```bash
PinionCore.Consoles.Chat1.Client \
  --router-host=127.0.0.1 \
  --router-port=8001
```

### 核心套件版本

- **PinionCore.Remote.Gateway**: 使用專案當前版本(需查閱 .csproj 或套件管理)
- **PinionCore.Network**: 使用專案當前版本
- **PinionCore.Utility**: 使用專案當前版本
- **Microsoft.Extensions.Configuration.CommandLine**: 建議使用 8.x 版本(相容 .NET 8.0)

---

**下一步**:執行第 0 階段研究,產生 `research.md` 文件。
