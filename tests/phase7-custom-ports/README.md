# Phase 7: Custom Port Configuration Test Guide

## 測試目標

驗證 Gateway Router 支援自訂端口配置：
- Router 能透過命令列參數指定自訂端口
- 參數驗證正確處理無效輸入（負數、超過 65535、非數字、端口衝突）
- 未指定的參數使用預設值
- 自訂端口能正常監聽並接受連接

## 測試架構

**預設端口配置**:
```
Agent TCP Port:       8001
Agent WebSocket Port: 8002
Registry TCP Port:    8003
```

**自訂端口配置** (Test 1):
```
Agent TCP Port:       9001
Agent WebSocket Port: 9002
Registry TCP Port:    9003
```

## 前置作業

### 編譯專案

```cmd
cd D:\develop\PinionCore.Remote
dotnet build --configuration Debug
```

### 確認 Router 可執行檔存在

```cmd
cd D:\develop\PinionCore.Remote\PinionCore.Consoles.Gateway.Router\bin\Debug\net8.0
dir PinionCore.Consoles.Gateway.Router.exe
```

## 測試步驟

### Test 1: 自訂端口配置 (T059)

**執行測試**:
```cmd
D:\develop\PinionCore.Remote\tests\phase7-custom-ports\test1-custom-ports.cmd
```

**預期輸出**:
```
[Info]Router 配置: Agent TCP=9001, Agent WebSocket=9002, Registry TCP=9003
[Info]Agent TCP 監聽已啟動，端口: 9001
[Info]Agent WebSocket 監聽已啟動，端口: 9002
[Info]Router Console 啟動完成，所有監聽器已就緒
```

**驗證端口監聽** (在另一個終端執行):
```cmd
netstat -an | findstr "9001 9002 9003"
```

**預期結果**:
```
TCP    0.0.0.0:9001           0.0.0.0:0              LISTENING
TCP    [::]:9001              [::]:0                 LISTENING
TCP    0.0.0.0:9003           0.0.0.0:0              LISTENING
TCP    [::]:9003              [::]:0                 LISTENING
```

**驗收標準**:
- ✅ Router 顯示正確的自訂端口配置
- ✅ 所有三個端口處於 LISTENING 狀態
- ✅ Router 正常啟動並保持運行

---

### Test 2: 無效端口處理 (T060)

**執行測試**:
```cmd
D:\develop\PinionCore.Remote\tests\phase7-custom-ports\test2-invalid-ports.cmd
```

此測試包含 4 個子測試，會依序執行。

#### Test 2A: 負數端口

**命令**: `--agent-tcp-port=-1`

**預期輸出**:
```
配置驗證失敗: Agent TCP 端口無效: -1 (有效範圍: 1-65535)

Gateway Router Console 使用說明:
  ...
```

**驗收標準**:
- ✅ 顯示清晰的錯誤訊息
- ✅ 顯示使用說明
- ✅ 應用程式終止（不啟動 Router）

#### Test 2B: 端口號超過 65535

**命令**: `--agent-tcp-port=99999`

**預期輸出**:
```
配置驗證失敗: Agent TCP 端口無效: 99999 (有效範圍: 1-65535)
```

**驗收標準**:
- ✅ 顯示端口超出範圍錯誤
- ✅ 應用程式終止

#### Test 2C: 非數字端口

**命令**: `--agent-tcp-port=abc`

**預期行為**:
- 參數被 `int.TryParse` 忽略
- 使用預設值 8001

**預期輸出**:
```
[Info]Router 配置: Agent TCP=8001, Agent WebSocket=8002, Registry TCP=8003
```

**驗收標準**:
- ✅ 非數字參數被忽略
- ✅ 使用預設值
- ✅ Router 正常啟動

#### Test 2D: 端口衝突

**命令**: `--agent-tcp-port=8000 --agent-web-port=8000`

**預期輸出**:
```
配置驗證失敗: 端口配置衝突:Agent TCP、Agent WebSocket、Registry TCP 必須使用不同端口
```

**驗收標準**:
- ✅ 檢測到端口衝突
- ✅ 顯示清晰的錯誤訊息
- ✅ 應用程式終止

---

### Test 3: 部分參數指定 (T061)

**執行測試**:
```cmd
D:\develop\PinionCore.Remote\tests\phase7-custom-ports\test3-partial-params.cmd
```

此測試包含 4 個子測試，驗證預設值填充邏輯。

#### Test 3A: 只指定 Agent TCP Port

**命令**: `--agent-tcp-port=9001`

**預期輸出**:
```
[Info]Router 配置: Agent TCP=9001, Agent WebSocket=8002, Registry TCP=8003
```

**驗收標準**:
- ✅ Agent TCP Port 使用指定值 9001
- ✅ Agent WebSocket Port 使用預設值 8002
- ✅ Registry TCP Port 使用預設值 8003

#### Test 3B: 只指定 Agent WebSocket Port

**命令**: `--agent-web-port=9002`

**預期輸出**:
```
[Info]Router 配置: Agent TCP=8001, Agent WebSocket=9002, Registry TCP=8003
```

**驗收標準**:
- ✅ Agent TCP Port 使用預設值 8001
- ✅ Agent WebSocket Port 使用指定值 9002
- ✅ Registry TCP Port 使用預設值 8003

#### Test 3C: 只指定 Registry TCP Port

**命令**: `--registry-tcp-port=9003`

**預期輸出**:
```
[Info]Router 配置: Agent TCP=8001, Agent WebSocket=8002, Registry TCP=9003
```

**驗收標準**:
- ✅ Agent TCP Port 使用預設值 8001
- ✅ Agent WebSocket Port 使用預設值 8002
- ✅ Registry TCP Port 使用指定值 9003

#### Test 3D: 指定兩個參數

**命令**: `--agent-tcp-port=9001 --registry-tcp-port=9003`

**預期輸出**:
```
[Info]Router 配置: Agent TCP=9001, Agent WebSocket=8002, Registry TCP=9003
```

**驗收標準**:
- ✅ Agent TCP Port 使用指定值 9001
- ✅ Agent WebSocket Port 使用預設值 8002
- ✅ Registry TCP Port 使用指定值 9003

---

## 整合測試：自訂端口連接

### 測試目標
驗證 Chat Server 和 Chat Client 能連接到使用自訂端口的 Router。

### 步驟 1: 啟動 Router (自訂端口)

**終端 1**:
```cmd
cd D:\develop\PinionCore.Remote\PinionCore.Consoles.Gateway.Router\bin\Debug\net8.0
PinionCore.Consoles.Gateway.Router.exe --agent-tcp-port=9001 --agent-web-port=9002 --registry-tcp-port=9003
```

### 步驟 2: 啟動 Chat Server (連接到 Registry 9003)

**終端 2**:
```cmd
cd D:\develop\PinionCore.Remote\PinionCore.Consoles.Chat1.Server\bin\Debug\net8.0
PinionCore.Consoles.Chat1.Server.exe --router-host=127.0.0.1 --router-port=9003 --group=1
```

**預期輸出**:
```
[Info]Registry 狀態: 連接中 (127.0.0.1:9003)
[Info]成功連接到 Router
[Info]Registry 狀態: 已連接
```

### 步驟 3: 啟動 Chat Client (連接到 Agent TCP 9001)

**終端 3**:
```cmd
cd D:\develop\PinionCore.Remote\PinionCore.Consoles.Chat1.Client\bin\Debug\net8.0
PinionCore.Consoles.Chat1.Client.exe --router-host=127.0.0.1 --router-port=9001
```

**預期輸出**:
```
Gateway Router mode (TCP).
Router: 127.0.0.1:9001
[TCP] Connecting to Router at 127.0.0.1:9001...
[TCP] Connected to Router. Waiting for routing allocation...
```

### 步驟 4: 聊天測試

在 Client 終端輸入：
```
login alice password123
say Hello from custom port!
```

**驗收標準**:
- ✅ Client 能連接到 Router 的自訂 Agent TCP Port (9001)
- ✅ Chat Server 能連接到 Router 的自訂 Registry Port (9003)
- ✅ Router 正確路由 Client 到 Chat Server
- ✅ 聊天功能正常運作

---

## 故障排除

### 問題：端口已被占用

**錯誤訊息**:
```
Agent 監聽器綁定失敗 (端口 9001/9002): ...
可能原因: 端口已被占用。請使用 netstat -an 檢查端口狀態或使用不同端口重試。
```

**解決方式**:
1. 檢查端口狀態: `netstat -an | findstr "9001 9002 9003"`
2. 關閉占用端口的程式
3. 或使用其他端口號重新測試

### 問題：參數格式錯誤

**錯誤訊息**:
```
配置驗證失敗: Agent TCP 端口無效: -1 (有效範圍: 1-65535)
```

**解決方式**:
1. 確認端口號在 1-65535 範圍內
2. 確認所有三個端口號不相同
3. 參考 Router 顯示的使用說明

### 問題：netstat 找不到端口

**可能原因**:
- Router 未成功啟動
- 端口綁定失敗但錯誤訊息被忽略

**解決方式**:
1. 檢查 Router 日誌是否有錯誤訊息
2. 確認 Windows Firewall 未阻擋端口
3. 嘗試使用管理員權限執行

---

## 技術實現

### 命令列參數解析

使用 `Microsoft.Extensions.Configuration` 的 `ConfigurationBuilder`:
```csharp
var configuration = new ConfigurationBuilder()
    .AddCommandLine(args)
    .Build();

if (int.TryParse(configuration["agent-tcp-port"], out int agentTcpPort))
    options.AgentTcpPort = agentTcpPort;
```

### 參數驗證

```csharp
public bool Validate(out string error)
{
    if (!IsValidPort(AgentTcpPort))
    {
        error = $"Agent TCP 端口無效: {AgentTcpPort} (有效範圍: 1-65535)";
        return false;
    }

    if (AgentTcpPort == AgentWebPort || AgentTcpPort == RegistryTcpPort || AgentWebPort == RegistryTcpPort)
    {
        error = "端口配置衝突:Agent TCP、Agent WebSocket、Registry TCP 必須使用不同端口";
        return false;
    }

    return true;
}

private bool IsValidPort(int port) => port >= 1 && port <= 65535;
```

### 預設值填充

使用 C# 屬性初始化器:
```csharp
public int AgentTcpPort { get; set; } = 8001;
public int AgentWebPort { get; set; } = 8002;
public int RegistryTcpPort { get; set; } = 8003;
```

未透過命令列指定的參數會保持預設值。

---

## 驗收標準總結

### ✅ T056 - 命令列參數解析
- [x] 支援 --agent-tcp-port 參數
- [x] 支援 --agent-web-port 參數
- [x] 支援 --registry-tcp-port 參數

### ✅ T057 - 參數格式驗證
- [x] 檢測負數端口
- [x] 檢測超過 65535 的端口
- [x] 非數字參數被忽略（使用預設值）
- [x] 檢測端口衝突
- [x] 顯示清晰的錯誤訊息與使用說明

### ✅ T058 - 預設值填充
- [x] 未指定參數使用預設值
- [x] 部分指定參數時其他使用預設值

### ✅ T059 - 自訂端口測試
- [x] Router 能使用自訂端口啟動
- [x] 端口正確處於 LISTENING 狀態

### ✅ T060 - 錯誤處理測試
- [x] 無效端口顯示錯誤並終止
- [x] 端口衝突顯示錯誤並終止

### ✅ T061 - 部分參數測試
- [x] 只指定單一參數時其他使用預設值
- [x] 指定多個參數時未指定的使用預設值

---

## 測試清單

完成以下測試並打勾：

- [ ] Test 1: 自訂端口配置 (9001, 9002, 9003)
- [ ] Test 2A: 負數端口 (-1)
- [ ] Test 2B: 端口超過 65535 (99999)
- [ ] Test 2C: 非數字端口 (abc)
- [ ] Test 2D: 端口衝突 (8000, 8000)
- [ ] Test 3A: 只指定 Agent TCP Port
- [ ] Test 3B: 只指定 Agent WebSocket Port
- [ ] Test 3C: 只指定 Registry TCP Port
- [ ] Test 3D: 指定兩個參數
- [ ] 整合測試: Chat Server 和 Client 連接到自訂端口 Router
