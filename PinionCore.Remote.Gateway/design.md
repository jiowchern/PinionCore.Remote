 # PinionCore Remote Gateway 架構文檔

 ## 概述

 PinionCore Remote Gateway
是一個高效的遊戲伺服器代理層，為分散式遊戲架構提供客戶端會話路由、服務發現與負載均衡功能。透過隱藏後端遊戲伺服器的 
地址，提供統一的遊戲服務入口點。

 ## 核心架構

 ### 設計目標

 - **服務隱藏**：隱藏後端遊戲伺服器 IP，增強安全性
 - **會話路由**：智能路由客戶端會話到適當的遊戲服務
 - **負載均衡**：在多個遊戲服務間分配客戶端連線
 - **服務發現**：動態註冊與發現可用的遊戲服務
 - **狀態管理**：維護客戶端會話與服務綁定狀態

 ### 系統拓撲

 ```
 [遊戲客戶端]
      ↓
 [Gateway Coordinator] ← 統一入口點
      ↓
 [Session Orchestrator] ← 會話協調與路由
      ↓
 [遊戲服務群組] ← 後端遊戲邏輯
 ```

 ## 核心組件

 ### 1. Gateway Coordinator (遊戲代理協調器)

 **職責**：作為系統的主要入口點，管理整個 Gateway 服務的生命週期

 **功能**：
 - 初始化會話協調器 (SessionOrchestrator)
 - 提供服務註冊介面 (IServiceRegistry)
 - 整合 PinionCore Remote 服務架構

 **關鍵特性**：
 - 統一的服務入口點
 - 管理 SessionOrchestrator 的生命週期
 - 提供 IService 介面供外部系統整合

 ### 2. Session Orchestrator (會話協調器)

 **職責**：Gateway 的核心路由引擎，負責會話管理與服務分配

 **功能**：
 - **會話管理**：追蹤活躍的客戶端會話 (IRoutableSession)
 - **服務綁定**：將客戶端會話綁定到特定遊戲服務
 - **狀態協調**：維護會話綁定狀態，處理異步操作
 - **負載均衡**：智能分配會話到可用服務

 **核心設計模式**：
 - **SessionBinding**：表示會話與服務的綁定關係
 - **ServiceRegistration**：管理已註冊的遊戲服務
 - **異步綁定**：支援非阻塞的會話分配流程

 ### 3. Service Entry Point (服務入口點)

 **職責**：為遊戲服務提供標準化的接入點

 **功能**：
 - 實作 IEntry 介面，整合 PinionCore Remote 架構
 - 管理客戶端綁定器 (IBinder) 的註冊與取消註冊
 - 橋接 SessionOrchestrator 與 PinionCore Remote 系統

 ### 4. Proxied Client (代理客戶端)

 **職責**：客戶端會話的抽象表示，支援路由與連線管理

 **功能**：
 - 實作 IRoutableSession 與 IConnectionManager 介面
 - 管理多群組會話綁定
 - 維護會話引用計數
 - 提供會話通知機制

 **設計特色**：
 - 支援一對多的會話群組映射
 - 自動管理會話生命週期
 - 線程安全的會話操作

 ### 5. Client Proxy (客戶端代理)

 **職責**：管理客戶端代理連線與代理生命週期

 **功能**：
 - 監聽 IConnectionManager 的會話變更事件
 - 為每個客戶端會話建立對應的 IAgent
 - 使用 ClientStreamAdapter 橋接會話與代理
 - 管理代理集合的動態更新

 ## 協議與通訊層

 ### 1. Game Lobby (遊戲大廳)

 **職責**：定義遊戲服務的標準介面

 **介面方法**：
 - `Join()`：客戶端加入，返回客戶端 ID
 - `Leave(uint clientId)`：客戶端離開
 - `ClientNotifier`：客戶端連線狀態通知器

 **改進特色**：
 - 使用 `ResponseStatus` 枚舉提供詳細的狀態回報
 - 支援非同步操作模式
 - 清晰的客戶端生命週期管理

 ### 2. Client Connection (客戶端連線)

 **職責**：抽象客戶端連線，支援請求-回應模式

 **介面方法**：
 - `Id`：唯一客戶端識別碼
 - `Request(byte[] payload)`：發送請求到客戶端
 - `ResponseEvent`：客戶端回應事件

 ### 3. Connection Manager (連線管理器)

 **職責**：管理客戶端連線集合，提供連線狀態通知

 **功能**：
 - 維護活躍客戶端連線集合
 - 提供連線新增/移除事件通知
 - 支援連線狀態查詢

 ## 傳輸層組件

 ### 1. Gateway Service (閘道服務)

 **職責**：統合客戶端服務與遊戲服務的雙向橋接

 **功能**：
 - 管理客戶端服務 (ClientService) 與遊戲服務 (GameService)
 - 處理客戶端加入/離開事件
 - 提供統一的 IService 介面

 ### 2. Connection Listener (連線監聽器)

 **職責**：實作 IGameLobby 與 IListenable，管理客戶端連線生命週期

 **功能**：
 - 自動分配客戶端 ID (使用 ClientIdGenerator)
 - 維護客戶端連線註冊表
 - 支援 IStreamable 事件通知
 - 整合 ClientStreamRegistry 進行串流管理

 ### 3. Connected Client (已連線客戶端)

 **職責**：客戶端連線的具體實作，支援 IClientConnection 與 IStreamable

 **功能**：
 - 封裝客戶端 ID 與串流操作
 - 實作請求-回應機制
 - 提供網路串流抽象

 ### 4. Client Stream Adapter (客戶端串流適配器)

 **職責**：橋接 IClientConnection 與 IStreamable，支援異步串流操作

 **功能**：
 - 非阻塞的串流讀寫
 - 與 ClientStreamRegistry 整合
 - 異步資料幫浦機制
 - 自動資源清理

 ### 5. Client Stream Registry (客戶端串流註冊表)

 **職責**：全域客戶端串流管理，支援跨組件的串流存取

 **功能**：
 - 執行緒安全的串流註冊/取消註冊
 - 串流橋接機制 (Bridge 模式)
 - 異步訊息佇列管理
 - 自動資源釋放

 ## 測試框架

 ### 1. Testable Agent (可測試代理)

 **職責**：為單元測試提供代理操作介面

 ### 2. Agent Test Harness (代理測試工具)

 **職責**：自動化代理生命週期管理，支援測試場景

 ### 3. Test Game Entry (測試遊戲入口)

 **職責**：測試環境下的遊戲服務模擬

 ### 4. Event Subscription Mock (事件訂閱模擬器)

 **職責**：模擬 IListenable 事件處理，支援測試驗證

 ## 工作流程

 ### 客戶端連線流程

 1. **連線建立**：客戶端連線到 Gateway Coordinator
 2. **會話分配**：SessionOrchestrator 分配 ProxiedClient
 3. **服務綁定**：根據負載均衡策略選擇遊戲服務
 4. **代理建立**：ClientProxy 為會話建立對應的 IAgent
 5. **串流橋接**：ClientStreamAdapter 建立雙向串流管道

 ### 服務註冊流程

 1. **服務啟動**：遊戲服務透過 ConnectionListener 建立 IGameLobby
 2. **註冊請求**：向 SessionOrchestrator 註冊服務與群組
 3. **事件訂閱**：SessionOrchestrator 訂閱服務的客戶端事件
 4. **狀態同步**：為現有會話分配新註冊的服務

 ### 異步綁定機制

 1. **立即返回**：`_TryAttach` 立即返回 true，不等待綁定完成
 2. **異步分配**：透過 `OnUserIdAssigned` 回調處理 UserId 分配
 3. **狀態協調**：PendingSessions 機制處理競爭條件
 4. **自動綁定**：ClientConnection 可用時自動完成綁定

 
