# 架構與模組總覽

[上一節：核心特色](core-features.md) | [下一節：快速開始](quick-start.md)

主要專案與角色：

- **PinionCore.Remote**
  - 核心介面與抽象：`IEntry`、`ISessionBinder`、`ISoul`
  - 狀態型別：`Value<T>`、`Property<T>`、`Notifier<T>`
- **PinionCore.Remote.Client**
  - `Proxy`、`IConnectingEndpoint`
  - 連線擴充：`AgentExtensions.Connect`
- **PinionCore.Remote.Server**
  - `Host`、`IListeningEndpoint`
  - 建立服務與監聽：`ServiceExtensions.ListenAsync`
- **PinionCore.Remote.Soul**
  - 伺服器 Session 管理（`SessionEngine`）
  - 更新迴圈：`ServiceUpdateLoop`
- **PinionCore.Remote.Ghost**
  - 客戶端 `Agent` 實作（`User`）
  - 封包編碼與處理
- **PinionCore.Remote.Standalone**
  - `ListeningEndpoint` 使用記憶體流模擬 Server/Client
- **PinionCore.Network**
  - `IStreamable` 介面、TCP/WebSocket Peer、封包讀寫
- **PinionCore.Serialization**
  - 預設序列化實作與型別描述（可替換）
- **PinionCore.Remote.Tools.Protocol.Sources**
  - Source Generator
  - 透過 `[PinionCore.Remote.Protocol.Creator]` 自動產生 `IProtocol`
- **PinionCore.Remote.Gateway**
  - Gateway / Router、多服務路由與版本共存（詳見該模組 README）
