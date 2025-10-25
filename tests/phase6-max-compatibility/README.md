# Phase 6: Maximum Compatibility Mode Test Guide

## 測試目標
驗證 Chat Server 的最大相容性連線模式：
- 同時支援 TCP 直連、WebSocket 直連、Gateway Router 路由
- 部分監聽器失敗時繼續運行其他監聽器
- 三種連線來源的客戶端能互相通訊

## 測試架構

### 最大相容模式
```
Direct TCP Client (23916) ──┐
                             │
Direct WebSocket Client ─────┼──→ Chat Server ←── Gateway Router (8003)
                             │                           ↑
Gateway Client ──────────────┘                           │
          (via Router 8001)                     Chat Client (Gateway Agent)
```

## 測試步驟

### 測試 1: 最大相容模式 (T053)

**目標**: 驗證 Chat Server 同時接受三種連線來源

1. **啟動 Router** (終端 1):
   ```cmd
   cd D:\develop\PinionCore.Remote\PinionCore.Consoles.Gateway.Router\bin\Debug\net8.0
   PinionCore.Consoles.Gateway.Router.exe
   ```

2. **啟動 Chat Server - 最大相容模式** (終端 2):
   ```cmd
   D:\develop\PinionCore.Remote\tests\phase6-max-compatibility\test1-max-compatibility.cmd
   ```

3. **驗證輸出**:
   ```
   ========================================
   Chat Server Started - 3 connection source(s) enabled:
     - TCP (port 23916)
     - WebSocket (port 23917)
     - Gateway Router (127.0.0.1:8003, Group 1)
   ========================================
   ```

4. **連接測試**:
   - 終端 3: TCP 直連 Client
     ```cmd
     cd D:\develop\PinionCore.Remote\PinionCore.Consoles.Chat1.Client\bin\Debug\net8.0
     PinionCore.Consoles.Chat1.Client.exe 127.0.0.1 23916
     ```
   
   - 終端 4: Gateway Router Client
     ```cmd
     cd D:\develop\PinionCore.Remote\PinionCore.Consoles.Chat1.Client\bin\Debug\net8.0
     PinionCore.Consoles.Chat1.Client.exe --router-host=127.0.0.1 --router-port=8001
     ```

5. **聊天測試**:
   - 兩個客戶端分別登入
   - 互相發送訊息
   - 預期：兩個客戶端能看到彼此的訊息

### 測試 2: TCP 單一模式 (T054)

**目標**: 驗證只啟用 TCP 時的回退行為

1. **啟動 Chat Server - TCP Only** (終端 1):
   ```cmd
   D:\develop\PinionCore.Remote\tests\phase6-max-compatibility\test2-tcp-only.cmd
   ```

2. **驗證輸出**:
   ```
   ========================================
   Chat Server Started - 1 connection source(s) enabled:
     - TCP (port 23916)
   ========================================
   ```

3. **連接測試**:
   ```cmd
   cd D:\develop\PinionCore.Remote\PinionCore.Consoles.Chat1.Client\bin\Debug\net8.0
   PinionCore.Consoles.Chat1.Client.exe 127.0.0.1 23916
   ```
   預期：能正常登入和聊天

### 測試 3: Gateway 單一模式 (T055)

**目標**: 驗證只啟用 Gateway 時的行為

1. **啟動 Router** (終端 1):
   ```cmd
   cd D:\develop\PinionCore.Remote\PinionCore.Consoles.Gateway.Router\bin\Debug\net8.0
   PinionCore.Consoles.Gateway.Router.exe
   ```

2. **啟動 Chat Server - Gateway Only** (終端 2):
   ```cmd
   D:\develop\PinionCore.Remote\tests\phase6-max-compatibility\test3-gateway-only.cmd
   ```

3. **驗證輸出**:
   ```
   ========================================
   Chat Server Started - 1 connection source(s) enabled:
     - Gateway Router (127.0.0.1:8003, Group 1)
   ========================================
   ```

4. **連接測試**:
   ```cmd
   cd D:\develop\PinionCore.Remote\PinionCore.Consoles.Chat1.Client\bin\Debug\net8.0
   PinionCore.Consoles.Chat1.Client.exe --router-host=127.0.0.1 --router-port=8001
   ```
   預期：能正常登入和聊天

### 測試 4: 部分監聽器失敗 (T052)

**目標**: 驗證錯誤處理機制

1. **占用端口 23916**:
   ```cmd
   # 先啟動一個 Server 占用 23916
   PinionCore.Consoles.Chat1.Server.exe --tcp-port=23916
   ```

2. **啟動第二個 Server**:
   ```cmd
   # 嘗試啟動最大相容模式 (23916 會失敗)
   PinionCore.Consoles.Chat1.Server.exe --tcp-port=23916 --web-port=23917 --router-host=127.0.0.1 --router-port=8003
   ```

3. **預期輸出**:
   ```
   [WARNING] TCP listener failed to start on port 23916: ...
   [OK] WebSocket listener started on port 23917
   [OK] Gateway mode initialized
   ========================================
   Chat Server Started - 2 connection source(s) enabled:
     - WebSocket (port 23917)
     - Gateway Router (127.0.0.1:8003, Group 1)
   ========================================
   ```

4. **驗證**: Server 繼續運行，WebSocket 和 Gateway 模式可用

## 驗收標準

### ✅ T053 - 最大相容模式
- [ ] 三種連線來源同時啟用
- [ ] 三種來源的客戶端能互相通訊

### ✅ T054 - TCP 單一模式
- [ ] 只有 TCP 監聽器啟用
- [ ] 日誌清晰顯示單一連線來源

### ✅ T055 - Gateway 單一模式
- [ ] 只有 Gateway 模式啟用
- [ ] 不開啟 TCP/WebSocket 端口

### ✅ T052 - 錯誤處理
- [ ] 部分監聽器失敗時顯示警告
- [ ] 其他監聽器繼續正常運行
- [ ] Server 不會因單一失敗而終止

## 故障排除

### 問題：端口已被占用
```
[WARNING] TCP listener failed to start on port 23916: Address already in use
```
**解決**: 使用 `netstat -an | findstr "23916"` 檢查端口，關閉占用進程

### 問題：Gateway 連接失敗
```
[WARNING] Gateway mode failed to initialize: ...
```
**解決**: 確認 Router 已啟動並監聽 8003 端口
