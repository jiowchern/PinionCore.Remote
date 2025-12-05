# PinionCore Remote – 簡介

[返回主 README](../../README.TC.md) | [下一節：核心特色](core-features.md)

**PinionCore Remote** 是一個以 C# 開發的「介面導向」遠端通訊框架。

你可以用 **介面（interface）** 定義遠端協議，伺服器實作這些介面，客戶端則像呼叫本地物件一樣呼叫它們；實際資料會透過 **TCP / WebSocket / Standalone（單機模擬）** 傳輸。

- 支援 **.NET Standard 2.1**（.NET 6/7/8、Unity 2021+）
- 支援 **IL2CPP 與 AOT**（需預先註冊序列化型別）
- 內建 **TCP、WebSocket、Standalone** 三種傳輸模式
- 透過 **Source Generator** 自動產生 `IProtocol` 實作，降低維護成本
- 以 **Value / Property / Notifier** 為核心抽象描述遠端行為與狀態
- 搭配 **PinionCore.Remote.Reactive** 可用 Rx 方式寫遠端流程

## 線上文件

- [DeepWiki](https://deepwiki.com/jiowchern/PinionCore.Remote)
- [OpenDeepWiki](https://opendeep.wiki/jiowchern/PinionCore.Remote/introduction?branch=master)
