# 範例與結語

[上一節：進階主題](advanced-topics.md) | [返回主 README](../../README.TC.md)

## 範例與測試

建議閱讀順序：

1. **PinionCore.Samples.HelloWorld.Protocols**
   - 基本 Protocol 與 `ProtocolCreator` 實作
2. **PinionCore.Samples.HelloWorld.Server**
   - `Entry`、`Greeter`、`Host` 用法
3. **PinionCore.Samples.HelloWorld.Client**
   - `Proxy`、`ConnectingEndpoint` 與 `QueryNotifier`
4. **PinionCore.Integration.Tests/SampleTests.cs**（強烈推薦）
   - 同時啟動 TCP / WebSocket / Standalone 三種端點
   - 使用 Rx (`SupplyEvent` / `RemoteValue`) 處理遠端呼叫
   - 詳細英文註解解釋背景處理迴圈的必要性
   - 驗證多種傳輸模式行為一致
5. **PinionCore.Remote.Gateway + PinionCore.Consoles.Chat1.***
   - Gateway 在實際專案中的組合與運作方式

---

## 結語

PinionCore Remote 的設計目標，是用「介面導向」把伺服器與客戶端之間的溝通，從繁瑣的封包格式、序列化與 ID 管理中抽離出來。你可以專注在 Domain 模型與狀態管理，其餘連線、供應 / 退供、版本檢查等細節交給框架處理。

無論是遊戲、即時服務、工具後端，或是透過 Gateway 串起多個服務，只要你的需求是「在不同進程或機器之間像呼叫本地介面一樣互動」，這個框架都可以作為基礎。

如果你第一次接觸這個專案，建議：

1. 先照著「快速開始」建立 Protocol / Server / Client 三個專案並跑起 Hello World。
2. 再閱讀 `PinionCore.Integration.Tests`（尤其 `SampleTests`）與 Gateway 範例。
3. 需要更進階能力時，再回頭看「進階主題」與對應程式碼檔案。

如果在使用過程中覺得文件有不清楚、範例不足，或遇到特殊需求，歡迎在 GitHub 開 Issue 討論，也歡迎 PR：補充說明、修正文案、增加小型範例或整合測試，都能讓下一個使用者更快上手。

希望 PinionCore Remote 能幫你省下處理網路細節的時間，讓你把心力放在真正重要的遊戲與應用程式設計上。
