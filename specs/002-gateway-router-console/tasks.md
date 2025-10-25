# 實作任務清單：Gateway Router Console Application

**功能分支**: `002-gateway-router-console`
**產生日期**: 2025-10-23
**規格文件**: [spec.md](./spec.md)
**實作計畫**: [plan.md](./plan.md)
**技術研究**: [research.md](./research.md)

## 總覽

本文件包含 Gateway Router Console Application 功能的完整實作任務清單,依據使用者故事優先級組織。每個使用者故事都是獨立可測試的增量功能。

**專案範疇**:
- 3 個主控台應用程式 (Router Console, Enhanced Chat Server, Enhanced Chat Client)
- Docker 容器化部署配置
- 支援多協議版本並存、負載平衡、自動重連

**技術棧**:
- .NET 8.0
- PinionCore.Remote.Gateway
- PinionCore.Network (TCP + WebSocket)
- PinionCore.Utility (Log + LogFileRecorder)
- Docker + Docker Compose

---

## 任務統計

- **總任務數**: 85 個任務
- **已完成**: 78 個任務 (91.8%)
- **待完成**: 7 個任務 (8.2%)
- **可平行任務**: 23 個 ([P] 標記)
- **使用者故事**: 8 個 (P1: 4 個, P2: 3 個, P3: 1 個)

**最新更新**: 2025-10-26 - T085 端到端整合測試完成

**Phase 分布**:
- Phase 1 (Setup): 5 個任務
- Phase 2 (Foundational): 8 個任務
- Phase 3 (US1 - P1): 12 個任務
- Phase 4 (US2 - P1): 10 個任務
- Phase 5 (US3 - P1): 8 個任務
- Phase 6 (US4 - P1): 12 個任務
- Phase 7 (US5 - P2): 6 個任務
- Phase 8 (US6 - P2): 4 個任務
- Phase 9 (US7 - P2): 6 個任務
- Phase 10 (US8 - P3): 6 個任務
- Phase 11 (Polish): 8 個任務

---

## MVP 範疇

**建議的 MVP 範疇 (User Story 1)**:
- Router Console 基本啟動與監聽
- 預設端口配置
- 日誌輸出 (stdout + 檔案)
- 端口衝突偵測
- 優雅關閉機制

**獨立測試標準**: 啟動 Router 應用程式，觀察監聽端口啟動日誌，使用 `netstat` 確認端口處於監聽狀態，發送 SIGTERM 確認優雅關閉。

---

## Phase 1: 專案設置

**目標**: 建立專案結構與基礎設施

- [X] T001 在 D:\develop\PinionCore.Remote\ 建立 PinionCore.Consoles.Gateway.Router 專案 (.NET 8.0 Console Application)
- [X] T002 將 Router 專案加入 PinionCore.sln 的 gateway 方案資料夾中
- [X] T003 在 Router 專案添加 NuGet 套件參考: PinionCore.Remote.Gateway, PinionCore.Network, PinionCore.Utility, Microsoft.Extensions.Configuration.CommandLine
- [X] T004 [P] 建立 docker/ 目錄結構 (在儲存庫根目錄)
- [X] T005 [P] 建立 specs/002-gateway-router-console/checklists/ 目錄 (若不存在)

---

## Phase 2: 基礎架構 (Foundational)

**目標**: 實作所有使用者故事所需的共用元件與基礎設施

**依賴**: Phase 1 完成

- [X] T006 實作 AgentWorker 類別在 PinionCore.Consoles.Gateway.Router/Workers/AgentWorker.cs (持續呼叫 HandlePackets 與 HandleMessage)
- [X] T007 實作 AgentWorkerPool 類別在 PinionCore.Consoles.Gateway.Router/Workers/AgentWorkerPool.cs (管理多個 AgentWorker 生命週期)
- [X] T008 [P] 實作 RouterOptions 類別在 PinionCore.Consoles.Gateway.Router/Configuration/RouterOptions.cs (命令列參數: agent-tcp-port, agent-web-port, registry-tcp-port)
- [X] T009 [P] 實作 ChatServerOptions 類別在 PinionCore.Consoles.Chat1.Server/Configuration/ChatServerOptions.cs (新增參數: router-host, router-port, group)
- [X] T010 [P] 實作 ChatClientOptions 類別在 PinionCore.Consoles.Chat1.Client/Configuration/ChatClientOptions.cs (新增參數: router-host, router-port)
- [X] T011 實作 GracefulShutdownHandler 在 PinionCore.Consoles.Gateway.Router/Infrastructure/GracefulShutdownHandler.cs (處理 SIGTERM/SIGINT，20 秒超時)
- [X] T012 實作 LoggingConfiguration 在 PinionCore.Consoles.Gateway.Router/Infrastructure/LoggingConfiguration.cs (配置 Log.Instance + LogFileRecorder)
- [X] T013 建立命令列參數解析邏輯在 Program.cs 使用 Microsoft.Extensions.Configuration.CommandLine

---

## Phase 3: User Story 1 - 啟動基本路由服務 (P1)

**使用者故事**: 運維人員需要快速啟動一個路由服務，使用預設端口配置即可開始提供 Registry 與 Agent 之間的路由功能。

**獨立測試標準**: 啟動 Router 應用程式並觀察監聽端口啟動日誌，驗證服務是否正常就緒。使用 `netstat` 或 `ss` 指令確認端口處於監聽狀態。

**依賴**: Phase 2 完成

### 實作任務

- [X] T014 [US1] 實作 RouterService 在 PinionCore.Consoles.Gateway.Router/Services/RouterService.cs (封裝 Gateway.Router 實例，使用 RoundRobinSelector)
- [X] T015 [US1] 實作 AgentListenerService 在 PinionCore.Consoles.Gateway.Router/Services/AgentListenerService.cs (管理 Agent TCP + WebSocket 監聽器)
- [X] T016 [US1] 實作 RegistryListenerService 在 PinionCore.Consoles.Gateway.Router/Services/RegistryListenerService.cs (管理 Registry TCP 監聽器)
- [X] T017 [US1] 在 Program.cs Main 方法實作 Router 初始化邏輯 (建立 RouterService 實例)
- [X] T018 [US1] 在 Program.cs 實作監聽器綁定邏輯 (Agent TCP: 8001, Agent WebSocket: 8002, Registry TCP: 8003)
- [X] T019 [US1] 實作端口配置驗證 (範圍 1-65535，數字格式，FR-011)
- [X] T020 [US1] 實作端口衝突偵測與錯誤處理 (綁定失敗時顯示清晰錯誤訊息並終止，FR-012)
- [X] T021 [US1] 實作日誌輸出邏輯 (Log.Instance.RecordEvent += Console.WriteLine，FR-014)
- [X] T022 [US1] 實作 LogFileRecorder 配置 (RouterConsole_yyyy_MM_dd_HH_mm_ss.log，FR-015)
- [X] T023 [US1] 實作 Router Update 迴圈 (持續呼叫 router.Registry.Update() 與 router.Session.Update())
- [X] T024 [US1] 整合 GracefulShutdownHandler 到 Program.cs (捕捉訊號，呼叫關閉流程)
- [X] T025 [US1] 實作優雅關閉邏輯 (關閉 Listeners → AgentWorkerPool → Router → Logs，FR-021, FR-022)

### 驗收測試

**測試腳本**:
```bash
# 編譯專案
cd D:\develop\PinionCore.Remote
dotnet build PinionCore.Consoles.Gateway.Router\PinionCore.Consoles.Gateway.Router.csproj --configuration Release

# 測試 1: 預設端口啟動
cd PinionCore.Consoles.Gateway.Router\bin\Release\net8.0
.\PinionCore.Consoles.Gateway.Router.exe
# 預期: stdout 顯示監聽端口資訊，產生日誌檔案

# 測試 2: 檢查端口監聽狀態
netstat -an | findstr "8001 8002 8003"
# 預期: 三個端口都處於 LISTENING 狀態

# 測試 3: 端口衝突測試
start /B .\PinionCore.Consoles.Gateway.Router.exe
.\PinionCore.Consoles.Gateway.Router.exe
# 預期: 第二個實例顯示端口占用錯誤並終止

# 測試 4: 優雅關閉測試
.\PinionCore.Consoles.Gateway.Router.exe
# 按 Ctrl+C
# 預期: 20 秒內完成關閉，日誌檔案完整寫入
```

---

## Phase 4: User Story 2 - Registry 連接與服務註冊 (P1)

**使用者故事**: 遊戲服務開發者需要將遊戲服務註冊到 Router，讓 Router 知道有哪些可用的遊戲服務實例，並能將客戶端路由到這些服務。

**獨立測試標準**: 啟動 Enhanced Chat Server (Registry Client) 連接到 Router，觀察 Router 端日誌顯示 Registry 連線建立與註冊成功訊息。

**依賴**: Phase 3 (US1) 完成

### 實作任務

- [X] T026 [P] [US2] 實作 RegistryClientService 在 PinionCore.Consoles.Chat1.Server/Services/RegistryClientService.cs (封裝 Gateway.Registry，提供連線管理)
- [X] T027 [P] [US2] 實作 ExponentialBackoffReconnector 在 PinionCore.Consoles.Chat1.Server/Services/ExponentialBackoffReconnector.cs (指數退避重連，1s-60s)
- [X] T028 [P] [US2] 實作 RegistryConnectionManager 在 PinionCore.Consoles.Chat1.Server/Services/RegistryConnectionManager.cs (管理連線狀態，偵測斷線)
- [X] T029 [US2] 在 Chat1.Server Program.cs 添加命令列參數解析 (router-host, router-port, group)
- [X] T030 [US2] 在 Chat1.Server Program.cs 實作 Registry Client 初始化 (當提供 router-host 時)
- [X] T031 [US2] 實作 Registry Agent 連接邏輯 (使用 Tcp.Connector 連接到 Router Registry 端點，FR-031)
- [X] T032 [US2] 實作 AgentWorker 啟動 (持續處理 registry.Agent.HandlePackets/HandleMessage)
- [X] T033 [US2] 在 Router 端添加 Registry 連接日誌 (使用 Log.WriteInfo 記錄連線建立與 Group ID，FR-018)
- [X] T034 [US2] 實作 Registry 斷線偵測與日誌記錄 (Router 端，FR-018)
- [X] T035 [US2] 實作 Registry 重連邏輯測試 (斷線後 10 秒內重連成功)

### 驗收測試

**測試腳本**:
```bash
# 測試 1: Registry 連接與註冊
# 終端 1: 啟動 Router
.\PinionCore.Consoles.Gateway.Router.exe

# 終端 2: 啟動 Chat Server (Registry 模式)
cd PinionCore.Consoles.Chat1.Server\bin\Release\net8.0
.\PinionCore.Consoles.Chat1.Server.exe --router-host=127.0.0.1 --router-port=8003 --group=1
# 預期: Chat Server 成功連接，Router 日誌顯示 Registry 連線建立

# 測試 2: 查看 Router 日誌
type RouterConsole_*.log | findstr "Registry"
# 預期: 顯示 Registry 連線事件，包含 Group ID

# 測試 3: Registry 斷線與重連
# 終端 2: 強制關閉 Chat Server (Ctrl+C)
# 觀察 Router 日誌顯示斷線事件
# 重新啟動 Chat Server
.\PinionCore.Consoles.Chat1.Server.exe --router-host=127.0.0.1 --router-port=8003 --group=1
# 預期: 10 秒內成功重連，Router 日誌記錄重連事件
```

---

## Phase 5: User Story 3 - Agent 連接與路由分配 (P1)

**使用者故事**: 遊戲玩家需要透過 Client (Agent) 連接到 Router，Router 自動將玩家路由到可用的遊戲服務實例，玩家無需知道後端服務的具體位置。

**獨立測試標準**: 啟動 Enhanced Chat Client (Agent) 連接到 Router，觀察連線建立、等待分配或成功路由到 Chat Server，並正常進行聊天功能。

**依賴**: Phase 4 (US2) 完成

### 實作任務

- [X] T036 [US3] Agent 連接處理 (PinionCore.Remote.Gateway 框架層已實作，測試驗證成功)
- [X] T037 [US3] Agent 路由分配邏輯 (PinionCore.Remote.Gateway 框架層已實作，測試驗證成功)
- [ ] T038 [US3] 在 Router 實作 Agent 等待匹配機制日誌 (可選增強功能)
- [ ] T039 [US3] 在 Router 實作路由分配成功日誌 (可選增強功能)
- [X] T040 [US3] 實作 Round-Robin 負載平衡測試 - 測試文檔: T040-LoadBalanceTest.md, Entry.cs 已增強客戶端連接統計日誌
- [ ] T041 [US3] 在 Router 實作 Agent 斷線處理日誌 (可選增強功能)
- [X] T042 [US3] 訊息轉發驗證 (Agent ↔ Router ↔ Registry 雙向通訊) - 測試成功
- [X] T043 [US3] 整合測試: 完整聊天功能透過 Router (登入、發送訊息、接收訊息) - 測試成功

### 驗收測試

**測試腳本**:
```bash
# 準備: 啟動 Router + Chat Server
# 終端 1: Router
.\PinionCore.Consoles.Gateway.Router.exe

# 終端 2: Chat Server
.\PinionCore.Consoles.Chat1.Server.exe --router-host=127.0.0.1 --router-port=8003 --group=1

# 測試 1: Agent 連接與路由 (需實作 Enhanced Chat Client)
# 終端 3: Chat Client (暫時跳過，等 US7 實作)

# 測試 2: 等待匹配機制
# 終端 1: 只啟動 Router (不啟動 Chat Server)
.\PinionCore.Consoles.Gateway.Router.exe
# 終端 3: 啟動 Chat Client 連接到 Router
# 預期: Client 連線保持，Router 日誌顯示 Agent 等待狀態

# 測試 3: 負載平衡測試
# 啟動 2 個 Chat Server (不同 group 或相同 group)
# 啟動 10 個 Chat Client
# 檢查 Router 日誌，驗證分配比例接近 5:5
```

---

## Phase 6: User Story 4 - 最大相容性 Chat Server 連線模式 (P1)

**使用者故事**: 遊戲服務開發者需要 Chat Server 能同時支援多種連線來源：直接 TCP 連線、直接 WebSocket 連線、以及透過 Gateway Router 路由的連線，無需修改業務邏輯。

**獨立測試標準**: 透過三種方式連接到同一個 Chat Server 實例：(1) 直接 TCP 連線；(2) 直接 WebSocket 連線；(3) 透過 Router 路由的連線。三種方式都應提供完整聊天功能。

**依賴**: Phase 4 (US2) 完成

### 實作任務

- [X] T044 [US4] CompositeListenable 已在 Phase 4 實作 (整合三重監聽來源)
- [X] T045 [US4] 條件啟用邏輯已在 Phase 4 實作 (依據命令列參數動態啟用)
- [X] T046 [US4] TCP 直連監聽器建立完成 (帶錯誤處理)
- [X] T047 [US4] WebSocket 直連監聽器建立完成 (帶錯誤處理)
- [X] T048 [US4] Gateway 路由監聽器整合完成 (帶錯誤處理)
- [X] T049 [US4] CompositeListenable 組合完成 (聚合三個 Listenable)
- [X] T050 [US4] 參數驗證完成 (至少提供一個連線模式)
- [X] T051 [US4] 連線模式日誌完成 (記錄啟用的模式列表)
- [X] T052 [US4] 部分監聽器啟動失敗處理完成 (try-catch + 警告日誌)
- [X] T053 [US4] 最大相容模式測試 - TCP + WebSocket + Gateway 三種連線並存，測試通過
- [X] T054 [US4] 回退模式測試 - TCP 單一模式，測試通過
- [X] T055 [US4] 純 Gateway 模式測試 - Gateway 單一模式，測試通過

### 驗收測試

**測試腳本**:
```bash
# 測試 1: 最大相容模式
.\PinionCore.Consoles.Chat1.Server.exe --tcp-port=23916 --web-port=23917 --router-host=127.0.0.1 --router-port=8003 --group=1
# 檢查日誌，預期: 顯示三種模式都已啟用

# 測試 2: TCP 直連模式
# 終端 1: 啟動 Chat Server (只 TCP)
.\PinionCore.Consoles.Chat1.Server.exe --tcp-port=23916
# 預期: 日誌指出未啟用 Gateway 模式

# 測試 3: 純 Gateway 模式
.\PinionCore.Consoles.Chat1.Server.exe --router-host=127.0.0.1 --router-port=8003 --group=1
# 預期: 不開啟 TCP/WebSocket 監聽端口

# 測試 4: 三種連線並存測試
# 啟動 Router
.\PinionCore.Consoles.Gateway.Router.exe
# 啟動 Chat Server (最大相容模式)
.\PinionCore.Consoles.Chat1.Server.exe --tcp-port=23916 --web-port=23917 --router-host=127.0.0.1 --router-port=8003 --group=1
# 啟動 3 個 Chat Client: 1 個 TCP 直連, 1 個 WebSocket 直連, 1 個透過 Router
# 預期: 三個客戶端都能登入並互相聊天
```

---

## Phase 7: User Story 5 - 自訂端口配置 (P2)

**使用者故事**: 運維人員需要在生產環境中部署多個 Router 實例或適應不同網路政策，必須能靈活配置監聽端口。

**獨立測試標準**: 透過命令列參數指定非預設端口啟動 Router，驗證服務監聽在指定端口上，並測試 Registry 與 Agent 能正確連接到自訂端口。

**依賴**: Phase 3 (US1) 完成

### 實作任務

- [x] T056 [P] [US5] 實作命令列參數解析邏輯 (--agent-tcp-port, --agent-web-port, --registry-tcp-port，FR-007, FR-008, FR-009)
- [x] T057 [P] [US5] 實作參數格式驗證 (負數、超過 65535、非數字，顯示錯誤與範例，FR-011, FR-023)
- [x] T058 [P] [US5] 實作預設值填充邏輯 (未指定的參數使用預設值，FR-011)
- [x] T059 [US5] 實作自訂端口配置測試 (使用 9001, 9002, 9003 啟動)
- [x] T060 [US5] 實作參數錯誤處理測試 (無效端口號，預期顯示錯誤並終止)
- [x] T061 [US5] 實作部分參數指定測試 (只指定 --agent-tcp-port，其他使用預設值)

### 驗收測試

**測試腳本**:
```bash
# 測試 1: 自訂端口啟動
.\PinionCore.Consoles.Gateway.Router.exe --agent-tcp-port=9001 --agent-web-port=9002 --registry-tcp-port=9003
# 檢查 netstat，預期: 9001, 9002, 9003 處於 LISTENING

# 測試 2: 參數格式錯誤
.\PinionCore.Consoles.Gateway.Router.exe --agent-tcp-port=-1
# 預期: 顯示錯誤訊息與正確格式範例，應用程式終止

.\PinionCore.Consoles.Gateway.Router.exe --agent-tcp-port=99999
# 預期: 顯示錯誤訊息 (超過 65535)

.\PinionCore.Consoles.Gateway.Router.exe --agent-tcp-port=abc
# 預期: 顯示錯誤訊息 (非數字)

# 測試 3: 部分參數指定
.\PinionCore.Consoles.Gateway.Router.exe --agent-tcp-port=9001
# 檢查日誌，預期: Agent TCP 使用 9001，其他使用預設值 (8002, 8003)
```

---

## Phase 8: User Story 6 - WebSocket 協議支援 (P2)

**使用者故事**: Web 應用程式開發者需要讓瀏覽器端的 Agent 能夠連接到 Router，因此需要 WebSocket 協議支援以繞過瀏覽器的跨域與協議限制。

**獨立測試標準**: 透過 WebSocket 測試工具連接到 Router 的 Agent WebSocket 端點，驗證連線建立與等待分配狀態。

**依賴**: Phase 3 (US1) 完成

### 實作任務

- [X] T062 [US6] WebSocket 監聽器已在 Phase 3 實作 (AgentListenerService 支援 TCP + WebSocket)
- [X] T063 [US6] WebSocket URL 綁定完成 (http://localhost:{port}/)
- [X] T064 [US6] GatewayConsole 支援 WebSocket 協議 (--websocket 參數)
- [X] T065 [US6] WebSocket 錯誤處理完成 (連接失敗顯示清晰錯誤)

### 驗收測試

**測試腳本**:
```bash
# 測試 1: WebSocket 連線
.\PinionCore.Consoles.Gateway.Router.exe
# 使用 WebSocket 測試工具 (如 wscat 或瀏覽器 WebSocket API)
# wscat -c ws://127.0.0.1:8002
# 預期: 連線成功，Router 日誌記錄 WebSocket 連線

# 測試 2: TCP 與 WebSocket 混合連線
# 同時啟動 TCP Agent 與 WebSocket Agent
# 預期: 兩種協議的客戶端都能被路由，日誌區分協議類型
```

---

## Phase 9: User Story 7 - Enhanced Chat Client Gateway 模式 (P2)

**使用者故事**: 遊戲玩家需要使用改造後的 Chat Client 連接到 Router，而不是直接連接到 Chat Server，體驗透明的服務發現與路由。

**獨立測試標準**: 透過兩種模式測試 Enhanced Chat Client：(1) 直連模式（連接到 Chat Server）；(2) Router 模式（連接到 Router）。兩種模式都應提供相同使用者體驗。

**依賴**: Phase 4 (US2) 完成

### 實作任務

- [X] T066 [P] [US7] 實作 GatewayConsole 使用 Gateway.Agent 連接到 Router Agent 端點
- [X] T067 [P] [US7] 實作 CommandLineOptions 與 CommandLineParser 解析命令列參數
- [X] T068 [US7] 在 Chat1.Client Program.cs 添加命令列參數解析 (router-host, router-port)
- [X] T069 [US7] 實作 Router 模式連接邏輯 (當提供 --router-host 時，FR-039)
- [X] T070 [US7] 實作回退到直連模式邏輯 (未提供 --router-host，FR-041)
- [X] T071 [US7] 實作連線失敗錯誤處理 (顯示訊息並退出，FR-043)

### 驗收測試

**測試腳本**:
```bash
# 準備: 啟動 Router + Chat Server
.\PinionCore.Consoles.Gateway.Router.exe
.\PinionCore.Consoles.Chat1.Server.exe --router-host=127.0.0.1 --router-port=8003 --group=1

# 測試 1: Router 模式
.\PinionCore.Consoles.Chat1.Client.exe --router-host=127.0.0.1 --router-port=8001
# 預期: Client 連接到 Router，被路由到 Chat Server，正常聊天

# 測試 2: 直連模式
.\PinionCore.Consoles.Chat1.Client.exe
# 預期: 提示輸入 Chat Server 的 host 與 port

# 測試 3: 連線失敗處理
# 不啟動 Router
.\PinionCore.Consoles.Chat1.Client.exe --router-host=127.0.0.1 --router-port=8001
# 預期: 顯示連線失敗訊息並退出
```

---

## Phase 10: User Story 8 - Docker 容器化部署 (P3)

**使用者故事**: 運維人員希望使用 Docker 容器來部署 Router 與 Chat Server，簡化部署流程並支援容器編排系統整合。

**獨立測試標準**: 透過 Dockerfile 構建 Router 與 Chat Server 映像檔、使用 Docker Compose 啟動完整環境（1 Router + 2 Chat Servers），並在本地使用 Chat Client 連接測試。

**依賴**: Phase 4 (US2) 完成

### 實作任務

- [X] T072 [P] [US8] 撰寫 Dockerfile.router 在 docker/Dockerfile.router (多階段構建，基於 mcr.microsoft.com/dotnet/runtime:8.0，FR-045)
- [X] T073 [P] [US8] 撰寫 Dockerfile.chatserver 在 docker/Dockerfile.chatserver (多階段構建，FR-046)
- [X] T074 [P] [US8] 撰寫 docker-compose.yml 在 docker/docker-compose.yml (1 Router + 2 Chat Servers，FR-048, FR-052, FR-053)
- [X] T075 [P] [US8] 撰寫 DOCKER.md 在 docker/DOCKER.md (構建映像、啟動容器、配置參數、查看日誌，FR-054)
- [X] T076 [P] [US8] 在 docker-compose.yml 配置容器間網路 (bridge 或自訂網路，FR-052)
- [X] T077 [P] [US8] 在 docker-compose.yml 配置 stop_grace_period: 30s (優雅關閉超時，FR-055)

### 驗收測試

**測試腳本**:
```bash
# 測試 1: 構建 Docker 映像
cd D:\develop\PinionCore.Remote\docker
docker build -f Dockerfile.router -t gateway-router:latest ..
docker build -f Dockerfile.chatserver -t chat-server:latest ..
# 預期: 映像構建成功

# 測試 2: Docker Compose 啟動
docker-compose up -d
# 預期: 3 個容器啟動 (router, chat-server-1, chat-server-2)

# 測試 3: 檢查容器狀態
docker-compose ps
# 預期: 所有容器狀態為 Up

# 測試 4: 查看日誌
docker-compose logs router
docker-compose logs chat-server-1
# 預期: 顯示監聽端口與 Registry 註冊訊息

# 測試 5: 本地 Client 連接測試
.\PinionCore.Consoles.Chat1.Client.exe --router-host=127.0.0.1 --router-port=8001
# 預期: Client 連接成功，被路由到其中一個 Chat Server

# 測試 6: 優雅關閉測試
docker-compose stop
# 預期: 容器在 30 秒內完成優雅關閉
```

---

## Phase 11: 打磨與跨切關注點

**目標**: 完善文件、錯誤處理與日誌品質

**依賴**: 所有 User Stories 完成

- [ ] T078 [P] 撰寫 README.md 在專案根目錄 (專案概述、快速開始、命令列參數說明、架構簡圖)
- [ ] T079 [P] 撰寫 ARCHITECTURE.md 在 specs/002-gateway-router-console/ (架構設計、元件關係、資料流程圖、最大相容性連線模式原理)
- [ ] T080 [P] 撰寫 TROUBLESHOOTING.md 在 specs/002-gateway-router-console/ (常見問題與除錯建議)
- [ ] T081 [P] 撰寫 LOGGING.md 在 specs/002-gateway-router-console/ (日誌系統使用說明、日誌格式、日誌等級)
- [X] T082 完善所有關鍵流程的日誌記錄 (啟動、連線、斷線、路由分配、模式切換、錯誤)
- [X] T083 實作錯誤處理標準化 (所有例外都有清晰錯誤訊息與日誌記錄) - 已完成 (2025-10-26)
- [X] T084 實作命令列 help 訊息 (--help 參數顯示完整使用說明與範例)
- [X] T085 執行端到端整合測試 (Router + 2 Chat Servers + 5 Chat Clients，驗證完整流程) - 已完成 (2025-10-26)

---

## 依賴關係圖

```
Phase 1 (Setup)
    ↓
Phase 2 (Foundational)
    ↓
    ├─→ Phase 3 (US1 - P1) ─→ Phase 7 (US5 - P2)
    │                       └→ Phase 8 (US6 - P2)
    │
    ├─→ Phase 4 (US2 - P1) ─→ Phase 5 (US3 - P1)
    │                       ├→ Phase 6 (US4 - P1)
    │                       ├→ Phase 9 (US7 - P2)
    │                       └→ Phase 10 (US8 - P3)
    │
    └─→ All User Stories Complete
                ↓
        Phase 11 (Polish)
```

---

## 平行執行機會

### Phase 2 (Foundational)
**可平行任務**: T008, T009, T010 (Options 類別互不依賴)

### Phase 4 (US2)
**可平行任務**: T026, T027, T028 (Registry Client 相關類別)

### Phase 7 (US5)
**可平行任務**: T056, T057, T058 (命令列參數處理)

### Phase 9 (US7)
**可平行任務**: T066, T067 (Client 相關服務類別)

### Phase 10 (US8)
**可平行任務**: T072, T073, T074, T075, T076, T077 (Docker 檔案互不依賴)

### Phase 11 (Polish)
**可平行任務**: T078, T079, T080, T081 (文件撰寫互不依賴)

---

## 實作策略

### 增量交付
1. **Sprint 1 (MVP)**: Phase 1-3 (US1)，建立基本 Router 功能
2. **Sprint 2**: Phase 4-5 (US2-US3)，實現 Registry 與 Agent 連接
3. **Sprint 3**: Phase 6 (US4)，實現最大相容性模式
4. **Sprint 4**: Phase 7-9 (US5-US7)，完善配置與 Client 模式
5. **Sprint 5**: Phase 10-11 (US8 + Polish)，容器化與文件

### 測試策略
- **每個 User Story** 完成後立即執行獨立測試
- **Phase 5** 完成後執行完整端到端測試
- **Phase 10** 完成後執行 Docker 環境整合測試
- **Phase 11** 完成後執行負載測試 (50 並發 Agent, 5 Registry)

---

## 格式驗證

**所有任務格式確認**:
- ✅ 所有任務使用 `- [ ]` checkbox 格式
- ✅ 所有任務有唯一 Task ID (T001-T085)
- ✅ 可平行任務標記 `[P]`
- ✅ User Story 任務標記 `[US1]`-`[US8]`
- ✅ 所有任務包含明確檔案路徑
- ✅ 任務依 User Story 優先級組織 (P1 → P2 → P3)

---

**任務清單產生完成**
**總任務數**: 85 個
**預估完成時間**: 4-5 個 Sprint (每個 Sprint 2 週)