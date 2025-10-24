# Feature Specification: Gateway Router Console Application

**Feature Branch**: `002-gateway-router-console`
**Created**: 2025-10-23
**Status**: Draft
**Input**: User description: "請用中文幫我撰寫規格
需求:
1. 在 Gate 方案資料夾中建立 PinionCore.Consoles.Gateway.Router 實現 PinionCore.Remote.Gateway 套件的的 Router 功能
2. 建立 tcp 與 web 的 agent 與 registry 監聽
3. 支援帶入命令 --agent-tcp-port= ,--agent-web-port= , --registry-tcp-port= (使用微軟的command line)
4. 建立 docker 部署文件
5. 在 PinionCore.Consoles.Chat1.Server 的基礎上實現 Registry Client (tcp) 用來連接已經運行的 Router
6. 在 PinionCore.Consoles.Chat1.Client 的基礎上實現 Agent (tcp) 用來連接已經運行的 Router"

## 功能概述

Gateway Router Console Application 是一個基於 PinionCore.Remote.Gateway 套件的路由服務主控台應用程式。此應用程式提供集中式路由功能，允許多個遊戲服務（Registry）註冊，並將客戶端連線（Agent）智能路由到適當的遊戲服務。專案包含三個主要部分：

1. **Router Console**: 核心路由服務，監聽 Registry 與 Agent 連線（TCP 與 WebSocket），執行路由分配邏輯
2. **Enhanced Chat Server**: 擴展 Chat1.Server，添加 Gateway 連接功能，支援三種連線來源：直接 TCP、直接 WebSocket、透過 Gateway 路由的連線（IStreamable）
3. **Enhanced Chat Client**: 擴展 Chat1.Client，添加 Gateway 連接模式，支援透過 Router 或直連到 Chat Server

此架構實現了服務發現與動態路由，支援多協議版本並存、負載平衡與彈性擴展。Router 採用等待匹配機制，當 Agent 或 Registry 連線時若暫無匹配對象，將保持連線等待直到匹配成功或使用者主動斷線。

## User Scenarios & Testing

### User Story 1 - 啟動基本路由服務 (Priority: P1)

運維人員需要快速啟動一個路由服務，使用預設端口配置即可開始提供 Registry 與 Agent 之間的路由功能。

**Why this priority**: 這是核心基礎設施，沒有 Router 就無法實現分散式架構。預設配置能讓使用者最快驗證基本路由功能。

**Independent Test**: 可透過啟動 Router 應用程式並觀察監聽端口啟動日誌，驗證服務是否正常就緒。使用 `netstat` 或 `ss` 指令確認端口處於監聽狀態。

**Acceptance Scenarios**:

1. **Given** Router 應用程式已編譯完成，**When** 運維人員執行應用程式不帶任何參數，**Then** 應用程式使用預設端口（Agent TCP: 8001, Agent Web: 8002, Registry TCP: 8003）啟動所有監聽服務，並在 stdout 與日誌檔案顯示監聽端點資訊與啟動時間
2. **Given** Router 服務已啟動，**When** 運維人員檢查端口占用狀態，**Then** 所有配置的端口都處於監聽狀態並接受連接
3. **Given** 指定的端口已被其他程式占用，**When** Router 嘗試啟動，**Then** 在 stdout 顯示清晰的錯誤訊息（包含端口號、占用原因），並立即終止應用程式
4. **Given** Router 啟動成功，**When** 查看應用程式目錄，**Then** 產生日誌檔案（格式：`RouterConsole_yyyy_MM_dd_HH_mm_ss.log`）記錄啟動事件與監聽器資訊

---

### User Story 2 - Registry 連接與服務註冊 (Priority: P1)

遊戲服務開發者需要將遊戲服務註冊到 Router，讓 Router 知道有哪些可用的遊戲服務實例，並能將客戶端路由到這些服務。

**Why this priority**: Registry 註冊是服務發現的前提，沒有註冊的服務無法被路由，這是分散式架構的核心流程。

**Independent Test**: 可透過啟動 Enhanced Chat Server（Registry Client）連接到 Router，觀察 Router 端日誌顯示 Registry 連線建立與註冊成功訊息。

**Acceptance Scenarios**:

1. **Given** Router 服務已啟動，**When** Enhanced Chat Server 使用 `--router-host=127.0.0.1 --router-port=8003` 透過 TCP 連接到 Router，**Then** 連線成功建立且 Chat Server 完成 Registry 註冊流程，Router 日誌記錄新 Registry 連線事件
2. **Given** Registry Client 已連接，**When** 查看 Router 的日誌，**Then** 顯示已註冊的服務資訊（包含 Group ID、Protocol Version 或連線識別資訊）
3. **Given** Registry Client 已連接且正在服務 Agent，**When** Registry Client 程式異常終止或網路斷線，**Then** Router 偵測到斷線並從可用服務列表中移除該服務，記錄斷線事件到 stdout 與日誌檔案
4. **Given** Registry Client 斷線後重新啟動，**When** 重新連接到 Router，**Then** Registry Client 成功重新註冊並恢復服務能力，Router 日誌記錄重連事件

---

### User Story 3 - Agent 連接與路由分配 (Priority: P1)

遊戲玩家需要透過 Client（Agent）連接到 Router，Router 自動將玩家路由到可用的遊戲服務實例，玩家無需知道後端服務的具體位置。

**Why this priority**: Agent 路由是使用者體驗的核心，這是整個路由系統存在的價值。沒有 Agent 連接，所有基礎設施都無用武之地。

**Independent Test**: 可透過啟動 Enhanced Chat Client（Agent）連接到 Router，觀察連線建立、等待分配或成功路由到 Chat Server，並正常進行聊天功能。

**Acceptance Scenarios**:

1. **Given** Router 已啟動且至少有一個 Registry 已註冊，**When** Enhanced Chat Client 使用 `--router-host=127.0.0.1 --router-port=8001` 透過 TCP 連接到 Router，**Then** 連線成功建立且 Router 自動將 Agent 路由到可用的 Registry，日誌記錄路由分配事件
2. **Given** Agent 已被路由到 Registry，**When** Agent 與 Registry 之間進行正常業務通訊（如聊天訊息），**Then** 訊息透過 Router 正確轉發，雙方能正常通訊如同直連
3. **Given** 多個 Registry 在同一個 Group 中註冊，**When** 多個 Agent 連接到 Router，**Then** Router 使用負載平衡策略（預設 Round-Robin）將 Agent 均勻分配到各個 Registry，日誌記錄每次分配決策
4. **Given** 沒有可用的 Registry，**When** Agent 連接到 Router，**Then** Agent 連線保持但處於等待狀態，Router 日誌記錄 Agent 等待狀態
5. **Given** Agent 已連接但尚未分配到 Registry（等待狀態），**When** 使用者主動斷開 Agent 連線，**Then** Agent 連線關閉，Router 日誌記錄 Agent 斷線事件
6. **Given** Agent 已被路由並正在使用服務，**When** Agent 斷線（主動或異常），**Then** Agent 連線關閉並退出應用，Router 日誌記錄 Agent 斷線事件

---

### User Story 4 - 最大相容性 Chat Server 連線模式 (Priority: P1)

遊戲服務開發者需要 Chat Server 能同時支援多種連線來源：直接 TCP 連線、直接 WebSocket 連線、以及透過 Gateway Router 路由的連線，無需修改業務邏輯。

**Why this priority**: 這是架構靈活性的關鍵，允許同一個 Chat Server 實例同時服務直連客戶端與透過 Router 路由的客戶端，最大化資源利用與部署彈性。

**Independent Test**: 可透過三種方式連接到同一個 Chat Server 實例：(1) 直接 TCP 連線；(2) 直接 WebSocket 連線；(3) 透過 Router 路由的連線。三種方式都應提供完整聊天功能。

**Acceptance Scenarios**:

1. **Given** Chat Server 使用參數 `--tcp-port=23916 --web-port=23917 --router-host=127.0.0.1 --router-port=8003 --group=1` 啟動，**When** 啟動完成，**Then** Chat Server 同時監聽 TCP 端口 23916、WebSocket 端口 23917，並作為 Registry Client 連接到 Router，日誌記錄三種模式的啟動狀態
2. **Given** Chat Server 以最大相容模式運行，**When** 一個客戶端透過 TCP 直連、一個透過 WebSocket 直連、一個透過 Router 路由連接，**Then** 所有三個客戶端都能登入並互相聊天，Chat Server 將所有連線視為統一的 IStreamable 處理
3. **Given** Chat Server 只提供 `--tcp-port` 參數（無 `--router-host`），**When** 啟動，**Then** 只開啟 TCP 直連模式，日誌明確指出未啟用 Gateway 模式
4. **Given** Chat Server 只提供 `--router-host` 參數（無 `--tcp-port` 與 `--web-port`），**When** 啟動，**Then** 只作為 Registry Client 連接到 Router，不開啟直連監聽端口

---

### User Story 5 - 自訂端口配置 (Priority: P2)

運維人員需要在生產環境中部署多個 Router 實例或適應不同網路政策，必須能靈活配置監聽端口。

**Why this priority**: 生產部署的關鍵需求，支援彈性配置能適應複雜網路環境、多實例部署與防火牆規則。

**Independent Test**: 可透過命令列參數指定非預設端口啟動 Router，驗證服務監聽在指定端口上，並測試 Registry 與 Agent 能正確連接到自訂端口。

**Acceptance Scenarios**:

1. **Given** Router 應用程式已編譯完成，**When** 運維人員使用命令列參數 `--agent-tcp-port=9001 --agent-web-port=9002 --registry-tcp-port=9003` 啟動，**Then** Router 在指定端口啟動所有監聽服務並在 stdout 與日誌檔案顯示完整端口配置資訊
2. **Given** 命令列參數格式錯誤或端口號無效（負數、超過 65535、非數字），**When** Router 嘗試啟動，**Then** 在 stdout 顯示清晰的參數驗證錯誤與正確格式範例，並終止應用程式
3. **Given** 只指定部分端口參數（如只指定 `--agent-tcp-port`），**When** Router 啟動，**Then** 使用預設值填充未指定的端口並正常啟動，日誌記錄使用的完整配置

---

### User Story 6 - WebSocket 協議支援 (Priority: P2)

Web 應用程式開發者需要讓瀏覽器端的 Agent 能夠連接到 Router，因此需要 WebSocket 協議支援以繞過瀏覽器的跨域與協議限制。

**Why this priority**: 對於需要支援 Web 客戶端的遊戲至關重要，擴展了框架的應用範圍到瀏覽器環境與 Unity WebGL 平台。

**Independent Test**: 可透過 WebSocket 測試工具連接到 Router 的 Agent WebSocket 端點，驗證連線建立與等待分配狀態。

**Acceptance Scenarios**:

1. **Given** Router 已啟動並開啟 Agent WebSocket 監聽，**When** WebSocket 客戶端透過 `ws://host:8002` 連接到 Agent WebSocket 端點，**Then** 連線成功建立且能被路由到可用的 Registry（當 Registry 存在時），Router 日誌記錄 WebSocket 連線事件
2. **Given** WebSocket Agent 已被路由，**When** 進行業務通訊（透過 PinionCore.Network.Web 發送訊息），**Then** 訊息透過 WebSocket 與 TCP 之間正確轉換與轉發，通訊如同 TCP 客戶端
3. **Given** 同時有 TCP 與 WebSocket Agent 連接，**When** 分配路由時，**Then** 兩種協議的客戶端都能正確被路由並與 Registry 通訊，日誌區分不同協議類型

---

### User Story 7 - Enhanced Chat Client Gateway 模式 (Priority: P2)

遊戲玩家需要使用改造後的 Chat Client 連接到 Router，而不是直接連接到 Chat Server，體驗透明的服務發現與路由。

**Why this priority**: 提供完整的端到端測試場景，驗證 Router、Registry、Agent 三方整合，也是使用者理解整體架構的最佳範例。

**Independent Test**: 可透過兩種模式測試 Enhanced Chat Client：(1) 直連模式（連接到 Chat Server）；(2) Router 模式（連接到 Router）。兩種模式都應提供相同使用者體驗。

**Acceptance Scenarios**:

1. **Given** Enhanced Chat Client 已編譯，**When** 使用命令列參數 `--router-host=127.0.0.1 --router-port=8001` 啟動，**Then** Client 作為 Agent 透過 TCP 連接到 Router，並等待或立即被路由到可用的 Chat Server
2. **Given** Enhanced Chat Client 未提供 `--router-host` 參數，**When** 啟動，**Then** Client 回退到傳統直連模式（提示使用者輸入 Chat Server 的 host 與 port）
3. **Given** Agent 模式下的 Chat Client 已連接並被路由，**When** 使用者進行登入與聊天操作，**Then** 所有功能運作如同直連模式，使用者感知不到中間的路由層
4. **Given** Agent 模式下的 Chat Client 連線失敗或被斷開，**When** 連線中斷發生，**Then** Client 顯示連線失敗訊息並退出應用

---

### User Story 8 - Docker 容器化部署 (Priority: P3)

運維人員希望使用 Docker 容器來部署 Router 與 Chat Server，簡化部署流程並支援容器編排系統整合。Chat Client 在本地開啟測試，不需要容器化。

**Why this priority**: 提升運維便利性與可擴展性，符合現代雲原生部署實踐，簡化多實例部署與環境隔離，但不影響核心功能。

**Independent Test**: 可透過 Dockerfile 構建 Router 與 Chat Server 映像檔、使用 Docker Compose 啟動完整環境（1 Router + 2 Chat Servers），並在本地使用 Chat Client 連接測試。

**Acceptance Scenarios**:

1. **Given** 提供的 Dockerfile（Router 與 Chat Server 各一個），**When** 運維人員執行 `docker build` 指令，**Then** 成功建立包含各應用程式的 Docker 映像檔
2. **Given** Router Docker 映像檔已建立，**When** 運維人員使用 `docker run` 並透過環境變數或命令列參數配置端口，**Then** Router 容器成功啟動且服務可從容器外部訪問，日誌輸出到 stdout
3. **Given** 提供的 Docker Compose 範例配置（包含 router、chat-server-1、chat-server-2），**When** 運維人員執行 `docker-compose up`，**Then** 完整環境啟動，兩個 Chat Server 都成功註冊到 Router，所有日誌可透過 `docker-compose logs` 查看
4. **Given** Docker Compose 環境正在運行，**When** 從容器外部（本地）使用 Chat Client 連接到 Router 的 Agent 端口（已映射到主機），**Then** Client 成功連接並被路由到其中一個 Chat Server，聊天功能正常運作
5. **Given** Docker 容器收到停止訊號（如 SIGTERM），**When** 容器關閉，**Then** 應用程式在 20 秒內完成優雅關閉（關閉連線、釋放資源），超過 20 秒則強制終止，日誌記錄關閉流程

---

### Edge Cases

- **端口衝突處理**: 當 Router 指定的端口已被占用時，在 stdout 顯示明確的錯誤訊息（包含端口號與占用原因）並立即終止應用程式，不嘗試使用其他端口
- **等待匹配超時**: Agent 或 Registry 連線後若長時間無匹配對象，連線會一直保持直到匹配成功或使用者主動斷線（無自動超時機制）
- **Registry 中途斷線**: 當 Registry 在服務過程中異常斷線，已路由到該 Registry 的 Agent 連線也會中斷；Registry 需實現重連邏輯以重新註冊到 Router
- **Agent 斷線處理**: Agent 斷線（主動或異常）後直接退出應用，不嘗試重連
- **Chat Server 混合模式下的連線識別**: Chat Server 需正確處理來自不同來源的 IStreamable 連線，但業務邏輯層無需區分連線來源
- **部分監聽器啟動失敗**: 當 Chat Server 的某個監聽器（如 TCP）啟動失敗但 Router 連線成功時，應繼續運行並記錄警告（而非終止整個應用）
- **負載平衡策略固定**: 目前使用 Round-Robin 策略，不支援動態切換其他策略
- **協議版本共存**: Router 支援不同協議版本的 Agent 與 Registry 同時連線，但只有版本匹配的 Agent 與 Registry 會被路由配對
- **大量並發連線**: 當同時有大量 Agent 或 Registry 連接時（如 100+ 並發），Router 的穩定性與效能表現需透過負載測試驗證
- **WebSocket 握手失敗**: 當 WebSocket 升級失敗時，PinionCore.Network.Web.Listener 會處理錯誤，Router 日誌記錄失敗事件
- **容器環境網路配置**: Docker 容器中 Router 應綁定 `0.0.0.0` 以允許跨容器與外部訪問；容器間通訊使用 Docker 網路，外部訪問使用端口映射
- **日誌檔案管理**: 日誌檔案持續寫入，不自動輪轉或限制大小（初版簡化實作，生產環境需外部日誌管理方案）
- **優雅關閉超時**: 應用程式收到 SIGTERM 後有 20 秒時間完成關閉流程，超過 20 秒則由容器管理系統或 OS 強制終止
- **命令列參數優先順序**: 命令列參數優於預設值

## Requirements

### Functional Requirements

#### Router Console 應用程式

- **FR-001**: Router Console 必須建立在 `D:\develop\PinionCore.Remote` 目錄下，專案名稱為 `PinionCore.Consoles.Gateway.Router`
- **FR-002**: Router Console 必須加入到 `PinionCore.sln` 的 `gateway` 方案資料夾中
- **FR-003**: Router Console 必須實現 `PinionCore.Remote.Gateway.Router` 類別，提供完整的路由服務能力
- **FR-004**: Router 必須提供 TCP 協議的 Agent 監聽端點，使用 `PinionCore.Network.Tcp.Listener` 實作
- **FR-005**: Router 必須提供 WebSocket 協議的 Agent 監聽端點，使用 `PinionCore.Network.Web.Listener` 實作
- **FR-006**: Router 必須提供 TCP 協議的 Registry 監聽端點，使用 `PinionCore.Network.Tcp.Listener` 實作
- **FR-007**: Router 必須支援透過命令列參數 `--agent-tcp-port` 配置 Agent TCP 監聽端口
- **FR-008**: Router 必須支援透過命令列參數 `--agent-web-port` 配置 Agent WebSocket 監聽端口
- **FR-009**: Router 必須支援透過命令列參數 `--registry-tcp-port` 配置 Registry TCP 監聽端口
- **FR-010**: Router 必須使用 `Microsoft.Extensions.Configuration.CommandLine` 或 `System.CommandLine` 處理命令列參數
- **FR-011**: Router 必須在啟動時驗證所有端口配置的有效性（範圍 1-65535、數字格式）
- **FR-012**: Router 必須在監聽器綁定失敗時（如端口占用），在 stdout 顯示清晰錯誤訊息並立即終止應用程式
- **FR-013**: Router 必須在所有監聽服務成功啟動後，使用 `PinionCore.Utility.Log` 輸出監聽端點資訊到 stdout 與日誌檔案
- **FR-014**: Router 必須使用 `PinionCore.Utility.Log.Instance.RecordEvent += System.Console.WriteLine` 將日誌輸出到 stdout
- **FR-015**: Router 必須使用 `PinionCore.Utility.LogFileRecorder` 將日誌輸出到檔案（命名格式：`RouterConsole_yyyy_MM_dd_HH_mm_ss.log`）
- **FR-016**: Router 必須實現負載平衡邏輯，使用 `ISessionSelectionStrategy` 分配 Agent 到可用的 Registry（預設使用 Round-Robin 策略）
- **FR-017**: Router 必須支援 Agent 與 Registry 的等待匹配機制：連線後若無匹配對象，保持連線直到匹配成功或使用者主動斷線
- **FR-018**: Router 必須在 Registry 連接或斷線時，更新可用服務列表並使用 `Log.Instance.WriteInfo()` 記錄事件
- **FR-019**: Router 必須在 Agent 連接、等待、路由分配時，使用 `Log.Instance.WriteInfo()` 記錄相關事件
- **FR-020**: Router 必須正確轉發 Agent 與 Registry 之間的雙向訊息流，確保通訊如同直連
- **FR-021**: Router 必須正確處理終止訊號（SIGTERM、SIGINT），在 20 秒內完成優雅關閉（關閉所有連線與監聽器）
- **FR-022**: Router 必須在優雅關閉時呼叫 `Log.Instance.Shutdown()` 與 `LogFileRecorder.Save()` / `LogFileRecorder.Close()` 確保日誌完整寫入
- **FR-023**: Router 必須在命令列參數錯誤時，在 stdout 顯示使用說明與參數格式範例，並終止應用程式

#### Enhanced Chat Server（最大相容性連線模式）

- **FR-024**: Enhanced Chat Server 必須基於 `PinionCore.Consoles.Chat1.Server` 專案擴展，保留所有原有聊天功能與命令列參數（`--tcp-port`、`--web-port`）
- **FR-025**: Enhanced Chat Server 必須新增命令列參數 `--router-host` 與 `--router-port` 用於配置 Router 連接資訊
- **FR-026**: Enhanced Chat Server 必須新增命令列參數 `--group` 指定註冊到 Router 的 Group ID
- **FR-027**: Enhanced Chat Server 必須支援最大相容性連線模式：當同時提供 `--tcp-port`、`--web-port`、`--router-host` 參數時，同時開啟三種連線來源
- **FR-028**: Enhanced Chat Server 必須將來自直接 TCP、直接 WebSocket、Gateway 路由的連線統一視為 `IStreamable`，在業務邏輯層無差別處理
- **FR-029**: Enhanced Chat Server 必須在只提供 `--tcp-port` 與 `--web-port` 時，回退到傳統獨立模式（不連接 Router）
- **FR-030**: Enhanced Chat Server 必須在只提供 `--router-host` 時，僅作為 Registry Client 連接到 Router，不開啟直連監聽端口
- **FR-031**: Enhanced Chat Server 必須在 Registry 模式下，使用 `PinionCore.Network.Tcp.Connector` 透過 TCP 連接到 Router 的 Registry 端點
- **FR-032**: Enhanced Chat Server 必須在成功連接到 Router 後，完成 Registry 註冊流程（透過 PinionCore.Remote.Gateway 定義的協議）
- **FR-033**: Enhanced Chat Server 必須在 Registry 模式下，實現或整合 `ILineAllocatable` 介面，提供 Stream 分配與回收能力
- **FR-034**: Enhanced Chat Server 必須在 Registry 模式下，當與 Router 連線中斷時，實現重連邏輯（如指數退避重試）
- **FR-035**: Enhanced Chat Server 必須使用 `PinionCore.Utility.Log` 記錄連線模式、連線狀態、錯誤等事件，輸出到 stdout 與檔案
- **FR-036**: Enhanced Chat Server 必須在部分監聽器啟動失敗時，記錄警告但繼續運行其他成功的監聽器（除非所有監聽器都失敗）

#### Enhanced Chat Client（Agent 模式）

- **FR-037**: Enhanced Chat Client 必須基於 `PinionCore.Consoles.Chat1.Client` 專案擴展，保留所有原有聊天功能
- **FR-038**: Enhanced Chat Client 必須新增命令列參數 `--router-host` 與 `--router-port` 配置 Router 連接資訊
- **FR-039**: Enhanced Chat Client 必須在 Router 模式下，使用 `PinionCore.Network.Tcp.Connector` 透過 TCP 連接到 Router 的 Agent 端點
- **FR-040**: Enhanced Chat Client 必須在成功連接到 Router 後，等待 Router 自動分配到可用的 Registry
- **FR-041**: Enhanced Chat Client 必須支援回退到直連模式（當未提供 `--router-host` 時），直接連接到 Chat Server 的 IP 與端口
- **FR-042**: Enhanced Chat Client 必須在 Router 模式與直連模式下都提供一致的使用者體驗（登入、聊天、命令介面）
- **FR-043**: Enhanced Chat Client 必須在連線失敗或斷開時，顯示有意義的錯誤訊息並退出應用（不實現自動重連）
- **FR-044**: Enhanced Chat Client 必須使用 `PinionCore.Utility.Log` 記錄連線模式、連線狀態、錯誤等事件，輸出到 stdout 與檔案

#### Docker 部署

- **FR-045**: 必須提供 Router Console 的 Dockerfile，基於 .NET 8.0 Runtime 映像（使用多階段構建優化）
- **FR-046**: 必須提供 Enhanced Chat Server 的 Dockerfile，基於 .NET 8.0 Runtime 映像（使用多階段構建優化）
- **FR-047**: Enhanced Chat Client 不需要 Dockerfile（用於本地測試）
- **FR-048**: 必須提供 Docker Compose 配置檔案，包含至少：1 Router + 2 Chat Server 的完整部署範例
- **FR-049**: Docker 映像檔必須包含應用程式的所有執行時依賴項（.NET Runtime、PinionCore 相關套件）
- **FR-050**: Docker 容器必須支援透過環境變數或命令列參數配置端口與連線資訊
- **FR-051**: Docker 容器必須將日誌輸出到標準輸出（stdout），日誌檔案可選地寫入容器內（但主要透過 stdout 收集）
- **FR-052**: Docker Compose 配置必須正確設置容器間網路（使用 bridge 或自訂網路），允許 Chat Server 與 Router 之間的 TCP 通訊
- **FR-053**: Docker Compose 配置必須將 Router 的 Agent 端口映射到主機，允許本地 Chat Client 連接
- **FR-054**: 必須提供 Docker 部署文件（DOCKER.md），說明如何構建映像、啟動容器、配置參數、查看日誌、連接測試
- **FR-055**: Docker 容器必須在收到 SIGTERM 訊號後 20 秒內完成優雅關閉，超過時間則由容器管理系統強制終止

### 預設配置 (Assumptions)

#### Router Console

- **Agent TCP 預設端口**: 8001
- **Agent WebSocket 預設端口**: 8002
- **Registry TCP 預設端口**: 8003
- **監聽 IP 位址**: 所有可用介面 (0.0.0.0)
- **負載平衡策略**: Round-Robin（使用 `PinionCore.Remote.Gateway.Hosts.RoundRobinSelector`）
- **優雅關閉超時時間**: 20 秒
- **日誌檔案命名**: `RouterConsole_yyyy_MM_dd_HH_mm_ss.log`
- **日誌輸出**: stdout + 檔案（使用 `PinionCore.Utility.Log` + `LogFileRecorder`）
- **等待匹配超時**: 無（保持連線直到匹配或使用者斷線）

#### Enhanced Chat Server

- **Router 模式 Group ID 預設值**: 1
- **直連 TCP 預設端口**: 23916（延續 Chat1.Server 預設值）
- **直連 Web 預設端口**: 23917（延續 Chat1.Server 預設值）
- **重連策略**: 實現自動重連（如指數退避，最大重試次數或時間可配置）
- **日誌檔案命名**: `ChatServer_yyyy_MM_dd_HH_mm_ss.log`
- **日誌輸出**: stdout + 檔案（使用 `PinionCore.Utility.Log` + `LogFileRecorder`）

#### Enhanced Chat Client

- **預設模式**: 直連模式（需使用者手動輸入 host 與 port）
- **Router 模式行為**: 連接後等待路由分配，無超時限制（使用者可主動斷線）
- **斷線後行為**: 顯示錯誤訊息並退出應用（不自動重連）
- **日誌檔案命名**: `ChatClient_yyyy_MM_dd_HH_mm_ss.log`
- **日誌輸出**: stdout + 檔案（使用 `PinionCore.Utility.Log` + `LogFileRecorder`）

### Key Entities

- **Router**: 路由服務的核心實體，管理 Registry 註冊與 Agent 路由分配邏輯，基於 `PinionCore.Remote.Gateway.Router` 實作
- **Registry Client**: 遊戲服務實例（如 Enhanced Chat Server），透過 TCP 連接到 Router 並註冊自己的可用性，提供實際的業務邏輯
- **Agent**: 客戶端連接（如 Enhanced Chat Client），透過 TCP 連接到 Router 並被路由到可用的 Registry Client，進行業務通訊
- **IStreamable**: 統一的連線抽象，代表來自 TCP、WebSocket 或 Gateway 路由的雙向資料流，是 Chat Server 處理所有連線的統一介面
- **SessionCoordinator**: Router 內部的路由協調器，根據協議版本與 Group ID 分配 Agent 到 Registry（來自 PinionCore.Remote.Gateway）
- **Line**: 虛擬 Stream 配對，包含 Frontend（連接到 Agent）與 Backend（連接到 Registry），實現訊息轉發（來自 PinionCore.Remote.Gateway）
- **ILineAllocatable**: Registry Client 必須實現的介面，提供 Stream 分配與回收能力（定義於 PinionCore.Remote.Gateway）
- **ISessionSelectionStrategy**: 負載平衡策略介面，決定如何從多個可用 Registry 中選擇一個進行路由（定義於 PinionCore.Remote.Gateway）
- **Log**: 原生日誌類別（來自 PinionCore.Utility），提供非同步日誌記錄與多輸出支援（stdout + 檔案）
- **LogFileRecorder**: 日誌檔案記錄器（來自 PinionCore.Utility），處理日誌檔案的建立、寫入與關閉

## Success Criteria

### Measurable Outcomes

- **SC-001**: 運維人員能在 30 秒內完成 Router 的基本配置與啟動（從執行指令到所有監聽器就緒）
- **SC-002**: Router 能同時穩定處理至少 50 個並發 Agent 連線與 5 個 Registry 連線而不發生錯誤或效能降級
- **SC-003**: Registry Client 從啟動到成功註冊到 Router 的時間不超過 3 秒
- **SC-004**: Agent 從連接到 Router 到被分配到 Registry 並可開始業務通訊的時間不超過 2 秒（當 Registry 已存在時）
- **SC-005**: 訊息透過 Router 轉發的額外延遲低於 10 毫秒（相比直連模式）
- **SC-006**: 當 Registry 異常斷線時，Router 能在 1 秒內偵測到並從可用列表中移除，日誌正確記錄事件
- **SC-007**: Registry Client 斷線後能在 10 秒內成功重新連接到 Router 並恢復服務（假設 Router 仍在運行）
- **SC-008**: Chat Server 在最大相容性模式下，能同時處理來自三種來源的連線（直接 TCP、直接 WebSocket、Gateway 路由），總計至少 30 個並發連線
- **SC-009**: 命令列參數錯誤時，100% 情況下在 stdout 顯示清晰的錯誤訊息與使用說明，無靜默失敗
- **SC-010**: Docker Compose 完整環境（Router + 2 Chat Servers）從啟動到所有服務就緒時間不超過 20 秒
- **SC-011**: 在 Round-Robin 負載平衡下，當有 2 個 Registry 時，10 個 Agent 的分配誤差不超過 ±1（即 5±1 vs 5±1 的分配）
- **SC-012**: Router 在收到終止訊號後 20 秒內完成優雅關閉，所有連線正確釋放，日誌完整寫入檔案
- **SC-013**: 90% 的開發者能在不查看詳細文件的情況下，僅透過 help 訊息與 Docker Compose 範例完成首次部署與測試
- **SC-014**: Enhanced Chat Client 在 Router 模式與直連模式下，使用者操作體驗完全一致（相同命令、相同輸出格式）
- **SC-015**: 所有關鍵事件（啟動、連線、斷線、路由分配、錯誤）都能在日誌檔案與 stdout 中查詢到，包含時間戳記與上下文資訊

## Out of Scope

以下功能明確不在此次規格範圍內：

- **版本不匹配拒絕機制**: Router 不主動拒絕版本不匹配的連線，而是保持連線等待匹配（已明確此為設計決策）
- **Agent 自動重連**: Agent 斷線後直接退出應用，不實現自動重連邏輯
- **多 Router 協調**: 多個 Router 實例之間的協調、狀態同步與高可用性
- **持久化狀態**: Router 重啟後恢復 Registry 註冊狀態或 Agent 連接狀態
- **認證與授權**: Registry 或 Agent 連接時的身份驗證、權限控制
- **加密通訊**: TLS/SSL 加密支援（假設在反向代理層處理或內部網路不需加密）
- **動態負載平衡策略配置**: 透過配置檔案或 API 切換負載平衡策略（目前固定 Round-Robin）
- **監控與指標**: Prometheus、Grafana 等監控系統整合，效能指標暴露
- **管理 API**: 用於查詢 Router 狀態、Registry 列表、Agent 連接數的 REST/gRPC API
- **日誌輪轉與管理**: 自動日誌檔案輪轉、壓縮、清理（需外部工具或未來版本）
- **訊息佇列**: 當 Registry 暫時不可用時緩存 Agent 訊息
- **熱更新**: 在不中斷服務的情況下更新 Router 配置或協議版本
- **Kubernetes Helm Chart**: 超出 Docker Compose 範圍的 Kubernetes 原生部署支援
- **多 Group 智能路由**: Agent 主動選擇 Group（目前由 Router 根據可用 Registry 自動分配）
- **流量控制與限流**: 防止單一 Agent 或 Registry 過度消耗資源
- **協議版本轉換**: Router 不負責協議版本之間的轉換或適配（只做匹配路由）
- **WebSocket 壓縮**: WebSocket 訊息的壓縮支援（預設不壓縮）
- **Chat Client 容器化**: Enhanced Chat Client 僅用於本地測試，不提供 Dockerfile

## Assumptions

- 使用者已安裝 .NET SDK 8.0 或更高版本用於開發與編譯
- .NET Runtime 8.0 或更高版本用於執行（包含 Docker 容器內）
- Docker 環境已正確安裝與配置（Docker Engine 20.10+、Docker Compose v2）
- 網路環境允許指定端口的 TCP 與 WebSocket 通訊，無防火牆阻擋
- Chat1.Server 與 Chat1.Client 專案已存在且可編譯運行
- PinionCore.Remote.Gateway 套件已正確安裝與引用，並理解其單機測試範例
- PinionCore.Network 套件提供的 TCP 與 WebSocket 實作穩定可用（Tcp.Listener、Tcp.Connector、Web.Listener）
- PinionCore.Utility 套件提供 Log 與 LogFileRecorder 類別用於日誌功能
- 命令列參數使用標準格式（如 `--key=value` 或 `--key value`）
- 應用程式將部署在 Linux 或 Windows Server 環境（支援 .NET 8+ 執行時與訊號處理）
- Docker 容器將運行在支援 .NET 的基礎映像（如 `mcr.microsoft.com/dotnet/runtime:8.0`）
- 使用者具備基本的命令列操作、Docker 使用與網路概念知識
- WebSocket 實作符合 RFC 6455 標準（PinionCore.Network.Web 已實作）
- Router 部署在穩定的網路環境中，不需要處理高頻率的網路抖動
- Registry Client 與 Agent 都信任 Router（內部網路環境，無需額外安全驗證）
- 開發者需要先理解 PinionCore.Network 與 PinionCore.Remote.Client/Server 套件與 PinionCore.Remote.Gateway 的對接方式（目前只有單機模式範例）
- 嚴禁修改原生 PinionCore 套件的程式碼（但添加 Log 呼叫是允許的）
- PinionCore.sln 方案檔案已存在且包含 `gateway` 方案資料夾

## Dependencies

### Framework & Runtime

- **.NET 8.0 SDK**: 開發與編譯所需
- **.NET 8.0 Runtime**: 執行環境
- **Microsoft.Extensions.Configuration**: 配置系統，用於命令列參數解析
- **Microsoft.Extensions.Configuration.CommandLine**: 命令列參數提供者
- **System.CommandLine** (替代選項): 現代化的命令列解析函式庫

### PinionCore Packages

- **PinionCore.Remote**: 核心 Remote 框架
- **PinionCore.Remote.Server**: 伺服器端實作（用於 Chat Server 的直連模式）
- **PinionCore.Remote.Client**: 客戶端實作（用於 Chat Client）
- **PinionCore.Remote.Gateway**: Gateway 套件，定義 Router、Registry、Agent 抽象與實作
- **PinionCore.Network**: 網路層抽象，提供 TCP 與 WebSocket 實作（Tcp.Listener、Tcp.Connector、Web.Listener）
- **PinionCore.Serialization**: 序列化框架
- **PinionCore.Remote.Tools.Protocol.Sources**: Protocol 程式碼產生器
- **PinionCore.Utility**: 工具函式庫（Log、LogFileRecorder、Console、StageMachine、StatusMachine 等）

### Existing Projects (as Base)

- **PinionCore.Consoles.Chat1.Server**: Enhanced Chat Server 的基礎專案
- **PinionCore.Consoles.Chat1.Client**: Enhanced Chat Client 的基礎專案
- **PinionCore.Consoles.Chat1.Common**: 共用的 Protocol 與介面定義
- **PinionCore.Consoles.Chat1**: 共用的業務邏輯（Entry、User、Room 等）

### Docker

- **Docker Engine**: 容器執行環境（20.10 或更高）
- **Docker Compose**: 多容器編排工具（v2 或更高）
- **mcr.microsoft.com/dotnet/runtime:8.0**: .NET Runtime 容器基礎映像
- **mcr.microsoft.com/dotnet/sdk:8.0**: .NET SDK 容器基礎映像（用於多階段構建）

## Risks & Mitigations

- **風險**: PinionCore.Remote.Gateway 目前只有單機模式測試範例，需要開發者自行理解如何與網路層對接
  - **緩解**: 在規劃階段深入研究 PinionCore.Network 與 PinionCore.Remote.Client/Server 的整合模式；參考 Chat1 專案的網路連線實作；編寫概念驗證（POC）程式碼驗證對接方式

- **風險**: Chat Server 最大相容性模式（同時支援三種連線來源）可能增加實作複雜度與錯誤處理難度
  - **緩解**: 使用清晰的抽象層將三種連線來源統一為 IStreamable；確保業務邏輯層完全無感知連線來源；編寫專門的測試覆蓋三種連線來源的並存情境

- **風險**: Registry Client 重連邏輯可能複雜，容易出現邊界情況（如重複註冊、狀態不一致）
  - **緩解**: 實作簡單的指數退避重連策略；確保每次重連前清理舊狀態；透過日誌詳細記錄重連過程；編寫專門的重連測試案例

- **風險**: Router 成為單點故障，若 Router 宕機則整個系統不可用
  - **緩解**: 初版接受此限制並在文件中明確說明；建議生產環境使用容器編排系統的自動重啟機制；未來版本可擴展到多 Router 高可用架構

- **風險**: 等待匹配機制可能導致 Agent 或 Registry 長時間處於未使用狀態，消耗資源
  - **緩解**: 在日誌中明確記錄等待狀態；文件中說明此行為並建議使用者在無法匹配時主動斷線；未來版本可考慮添加可選的超時機制

- **風險**: WebSocket 實作可能與某些瀏覽器或網路環境不相容
  - **緩解**: 使用 PinionCore.Network.Web 提供的標準實作，遵循 RFC 6455；提供 WebSocket 測試工具與除錯日誌；在文件中列出測試過的瀏覽器版本

- **風險**: 負載平衡策略可能在實際場景中不夠智能（Round-Robin 未考慮 Registry 實際負載）
  - **緩解**: 初版使用簡單的 Round-Robin；架構上已使用 `ISessionSelectionStrategy` 介面，方便未來擴展；在文件中說明策略限制

- **風險**: Docker 映像檔可能過大，影響部署速度
  - **緩解**: 使用多階段構建（multi-stage build）；使用 .NET Runtime 而非 SDK 作為執行環境基礎映像；在 .dockerignore 中排除不必要的檔案

- **風險**: 在高並發場景下，Router 的效能可能成為瓶頸
  - **緩解**: 在開發階段進行負載測試；在文件中明確說明效能限制與推薦的部署規模（如 50 並發 Agent 作為目標）

- **風險**: 優雅關閉邏輯可能無法正確處理所有連線狀態，導致資源洩漏
  - **緩解**: 實作 20 秒超時機制；確保即使部分連線無法正常關閉，應用程式仍能退出；進行關閉流程的專門測試；呼叫 `Log.Instance.Shutdown()` 與 `LogFileRecorder.Save()` 確保日誌完整

- **風險**: 日誌檔案無限增長可能佔滿磁碟空間
  - **緩解**: 在文件中明確說明日誌檔案不自動輪轉；建議生產環境使用外部日誌管理方案（如 logrotate、集中式日誌系統）；未來版本可添加日誌輪轉配置

## Notes

### 專案結構

- **PinionCore.Consoles.Gateway.Router**:
  - 路徑：`D:\develop\PinionCore.Remote\PinionCore.Consoles.Gateway.Router\`
  - 加入到 `PinionCore.sln` 的 `gateway` 方案資料夾
- **PinionCore.Consoles.Chat1.Server**: 需要擴展添加 Gateway 連接功能（建議直接修改或建立衍生類別）
- **PinionCore.Consoles.Chat1.Client**: 需要擴展添加 Gateway 連接功能（建議直接修改或建立衍生類別）

### 重要設計決策

1. **等待匹配機制**: Router 不主動拒絕版本不匹配或無可用對象的連線，而是保持連線等待，這簡化了實作但需要使用者理解此行為
2. **Agent 不重連**: Agent 斷線後直接退出應用，這簡化了客戶端邏輯，適合遊戲客戶端場景（使用者可重新啟動）
3. **Registry 重連**: Registry Client 實現自動重連以確保服務可用性，適合伺服器端場景
4. **最大相容性連線模式**: Chat Server 同時支援直連與 Gateway 路由，所有連線統一為 IStreamable，業務邏輯無差異
5. **嚴禁修改原生套件**: 所有功能必須透過組合與擴展實現，可添加 Log 但不修改套件邏輯
6. **20 秒優雅關閉**: 提供充足的時間完成關閉流程，適合容器環境與生產部署
7. **原生日誌系統**: 使用 PinionCore.Utility.Log 與 LogFileRecorder，避免引入第三方日誌函式庫

### 日誌使用模式

```csharp
// 初始化（在 Program.cs 或主類別）
var log = PinionCore.Utility.Log.Instance;
var fileRecorder = new PinionCore.Utility.LogFileRecorder("RouterConsole");

// 配置 stdout 輸出
log.RecordEvent += System.Console.WriteLine;

// 配置檔案輸出
log.RecordEvent += fileRecorder.Record;

// 寫入日誌
log.WriteInfo("Router started successfully");
log.WriteInfo(() => $"Listening on port {port}");
log.WriteDebug("Debug message with stack trace");

// 優雅關閉
fileRecorder.Save();
fileRecorder.Close();
log.Shutdown();  // 等待非同步佇列清空
```

### 架構設計原則

- **嚴禁使用 static class**: 遵循 PinionCore 框架規範，避免使用 static class 以支援多實例與測試
- **使用 PinionCore.Network 而非第三方**: 所有網路連線必須使用 PinionCore.Network 提供的抽象與實作
- **不整合 gRPC 等主流框架**: PinionCore.Remote 是原生網路框架，不需要也不應該整合其他 RPC 框架
- **遵循 Chat1 專案模式**: Enhanced Chat Server/Client 應遵循 Chat1 專案的架構模式（Entry、Console、State Machine 等）
- **IStreamable 統一抽象**: 所有連線來源（TCP、WebSocket、Gateway）在業務邏輯層統一為 IStreamable，實現最大相容性

### 開發建議

- **理解對接方式**: 優先研究如何用 PinionCore.Network 與 PinionCore.Remote.Client/Server 對接 PinionCore.Remote.Gateway（這是關鍵技術挑戰）
- **參考 Chat1 網路層**: `PinionCore.Consoles.Chat1.Server.CompositeListenable` 提供多協議監聽器模式，可擴展為三種連線來源的聚合
- **參考 Gateway 核心邏輯**: `PinionCore.Remote.Gateway.Hosts.SessionCoordinator` 與 `PinionCore.Remote.Gateway.Registrys.Server` 提供路由與註冊邏輯
- **使用原生日誌**: 所有專案統一使用 `PinionCore.Utility.Log` 與 `LogFileRecorder`，避免引入第三方日誌函式庫
- **添加充足的 Log**: 在關鍵流程（啟動、連線、斷線、路由分配、模式切換、錯誤）添加詳細日誌，方便除錯與運維
- **優雅關閉流程**: 確保呼叫 `Log.Instance.Shutdown()` 與 `LogFileRecorder.Save()/Close()` 以完整寫入日誌

### 未來擴展性考慮

- 預留 `ISessionSelectionStrategy` 介面以支援未來的其他負載平衡策略
- Router 內部使用清晰的抽象層，方便未來擴展到多 Router 協調架構
- 日誌輸出包含足夠的上下文資訊（如連線 ID、時間戳記、事件類型）以便未來整合監控系統
- Docker 部署為未來的 Kubernetes 部署打下基礎
- Chat Server 的最大相容性架構為未來支援更多連線來源（如 Unix Socket、Named Pipe）提供擴展性

### 測試策略

- **單元測試**: 對 Router 的核心邏輯（路由分配、負載平衡）進行單元測試（若可行）
- **整合測試**: 使用 Standalone 模式測試 Router、Registry、Agent 三方整合（若 Gateway 支援）
- **端到端測試**: 使用實際 TCP/WebSocket 連線測試完整流程（手動測試或自動化腳本）
- **混合連線測試**: 同時使用直連與 Gateway 路由連接到 Chat Server，驗證最大相容性模式
- **負載測試**: 模擬 50+ 並發 Agent 與 5+ Registry 連接，驗證效能與穩定性
- **容器測試**: 使用 Docker Compose 啟動完整環境，驗證容器化部署的可行性
- **重連測試**: 手動停止 Router 或 Registry，驗證重連邏輯與錯誤處理
- **優雅關閉測試**: 發送 SIGTERM 訊號，驗證 20 秒內正確關閉與日誌完整寫入

### 文件需求

- **README.md**: 專案概述、快速開始、命令列參數說明、架構簡圖、三種連線模式說明
- **ARCHITECTURE.md**: 架構設計、元件關係、資料流程圖、對接方式說明、最大相容性連線模式原理
- **DOCKER.md**: Docker 構建與部署指南、Docker Compose 使用說明、端口映射配置、日誌查看
- **TROUBLESHOOTING.md**: 常見問題（連線失敗、端口占用、版本不匹配、混合連線問題等）與除錯建議
- **LOGGING.md**: 日誌系統使用說明、日誌格式、日誌等級、日誌檔案位置、日誌管理建議
