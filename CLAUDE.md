# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 專案概述

PinionCore Remote 是一個 C# 伺服器-客戶端通訊框架，支援 Unity 與 .NET Standard 2.1+ 環境。透過介面進行物件導向的遠端通訊，降低協議維護成本。

## 常用指令

### 建置專案
```bash
dotnet restore
dotnet build --configuration Release --no-restore
```

### 執行測試
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutput=../CoverageResults/ /p:MergeWith="../CoverageResults/coverage.json" /p:CoverletOutputFormat="lcov%2cjson" -m:1
```

### 打包 NuGet 套件
```bash
dotnet pack --configuration Release --output ./nupkgs
```

### 執行單一專案測試
```bash
dotnet test [項目路徑].csproj
```

## 核心架構

### 專案結構
- **PinionCore.Remote**: 核心框架，定義基本介面與抽象
- **PinionCore.Remote.Server**: 伺服器端實作，包含 TCP 服務與連線管理
- **PinionCore.Remote.Client**: 客戶端實作，包含代理與連線器
- **PinionCore.Remote.Soul**: 伺服器端物件綁定管理
- **PinionCore.Remote.Ghost**: 客戶端遠端物件代理
- **PinionCore.Remote.Standalone**: 單機模式，無需網路的模擬環境
- **PinionCore.Network**: 底層網路抽象，定義 IStreamable 等介面
- **PinionCore.Serialization**: 序列化框架，處理資料轉換
- **PinionCore.Remote.Tools.Protocol.Sources**: 程式碼產生器，自動生成 IProtocol
- **PinionCore.Remote.Gateway**: API 閘道服務

### 關鍵設計模式

#### 1. 介面導向通訊
- 伺服器實作介面，客戶端透過相同介面呼叫
- 支援方法（Value<T>）、事件、屬性、Notifier
- 透過 IBinder 綁定伺服器物件，IAgent 查詢客戶端物件

#### 2. Protocol 生成機制
```csharp
[PinionCore.Remote.Protocol.Creator]
static partial void _Create(ref PinionCore.Remote.IProtocol protocol);
```
- 使用 Source Generator 自動產生通訊協議
- 必須定義在 static partial void 方法上
- 透過 ProtocolCreator.Create() 取得 IProtocol 實例

#### 3. 連線抽象化
- IStreamable: 客戶端資料流抽象
- IListenable: 伺服器端監聽器抽象
- 預設提供 TCP 實作，可自訂其他連線方式

#### 4. 序列化可擴展性
- ISerializable 介面允許自訂序列化
- 預設支援基礎型別與陣列
- IProtocol.SerializeTypes 提供需序列化的型別清單

#### 5. IDisposable 資源釋放模式

**使用 `_Dispose` 閉包模式處理延遲初始化資源**：

```csharp
public class MyService : IDisposable
{
    private bool _disposed = false;
    private System.Action _Dispose;  // 閉包捕獲清理邏輯

    public MyService()
    {
        _Dispose = () => { };  // 安全的預設空操作
    }

    public void Start(...)
    {
        // 創建具體類型的區域變數
        var concreteResource = new ConcreteType();
        concreteResource.Initialize();

        // 訂閱事件
        _interfaceField.SomeEvent += handler;

        // 設置清理邏輯（閉包捕獲區域變數）
        _Dispose = () =>
        {
            _interfaceField.SomeEvent -= handler;  // 取消訂閱
            concreteResource.Close();              // 調用具體方法
        };
    }

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            _Dispose();  // 統一調用清理邏輯
        }
        catch (Exception ex)
        {
            // 記錄錯誤
        }

        _disposed = true;
    }
}
```

**適用場景**：
- ✅ 資源在 `Start()` 等方法中延遲初始化（非建構子）
- ✅ 欄位類型是介面,但需要調用具體類型的方法（如 `Close()`）
- ✅ 需要取消事件訂閱,事件處理器引用需要保存
- ✅ 多個相關資源需要協調清理

**核心優點**：
- 閉包捕獲區域變數的具體類型,無需將其存為欄位
- 清理邏輯緊鄰初始化邏輯,便於維護
- 預設空操作確保安全性
- 簡化 `Dispose()` 實作,無需多重 null 檢查

## 開發流程

### 新增通訊介面
1. 在 Protocol 專案定義介面
2. 使用 PinionCore.Remote.Value<T> 作為非同步方法回傳型別
3. 重新建置以觸發程式碼產生

### 伺服器端實作
1. 實作 IEntry 介面作為進入點
2. 在 RegisterClientBinder 中使用 binder.Bind<T> 綁定物件
3. 透過 Provider.CreateTcpService 建立服務

### 客戶端實作
1. 透過 Provider.CreateTcpAgent 建立代理
2. 使用 agent.QueryNotifier<T>().Supply/Unsupply 監聽物件
3. 定期呼叫 HandleMessages() 與 HandlePackets()

### 測試方式
- 使用 Standalone 模式進行無網路測試
- 整合測試位於 PinionCore.Integration.Tests
- 單元測試依功能模組分布在各 *.Test 專案

## 範例專案參考
- PinionCore.Samples.HelloWorld.*: 基本客戶端-伺服器範例
- PinionCore.Remote/Sample: 更多使用情境範例

## 重要提醒
- 修改 Protocol 介面後需重新建置所有相依專案
- Unity WebGL 需自行實作 WebSocket 客戶端
- IL2CPP 與 AOT 環境下序列化型別需預先註冊
- Standalone 模式適合開發階段除錯，生產環境應使用網路模式
- 英文思考，結論用中文撰寫
- 這是一個網路通訊框架所以開發功能嚴禁使用 static class 
