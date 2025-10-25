# Docker 部署指南

Gateway Router Console Application 的 Docker 容器化部署文檔。

## 目錄

- [快速開始](#快速開始)
- [構建映像檔](#構建映像檔)
- [啟動容器](#啟動容器)
- [管理容器](#管理容器)
- [配置參數](#配置參數)
- [日誌查看](#日誌查看)
- [常見問題排解](#常見問題排解)

---

## 快速開始

### 前置需求

- Docker Engine 20.10+
- Docker Compose 2.0+
- 至少 2GB 可用磁碟空間

### 一鍵啟動

```bash
# 切換到 docker 目錄
cd D:\develop\PinionCore.Remote\docker

# 構建並啟動所有服務
docker-compose up -d

# 查看服務狀態
docker-compose ps

# 查看日誌
docker-compose logs -f
```

**預期結果**:
- `gateway-router` 容器監聽端口 8001 (TCP), 8002 (WebSocket), 8003 (Registry)
- `chat-server-1` 和 `chat-server-2` 透過內部網路連接到 Router

---

## 構建映像檔

### Router 映像檔

```bash
# 從專案根目錄執行
cd D:\develop\PinionCore.Remote

# 構建 Router 映像檔
docker build -f docker/Dockerfile.router -t gateway-router:latest .

# 驗證映像檔
docker images | grep gateway-router
```

**輸出範例**:
```
gateway-router   latest   abc123def456   2 minutes ago   200MB
```

### Chat Server 映像檔

```bash
# 從專案根目錄執行
cd D:\develop\PinionCore.Remote

# 構建 Chat Server 映像檔
docker build -f docker/Dockerfile.chatserver -t chat-server:latest .

# 驗證映像檔
docker images | grep chat-server
```

**輸出範例**:
```
chat-server      latest   def456ghi789   1 minute ago    195MB
```

### 使用 Docker Compose 構建

```bash
cd D:\develop\PinionCore.Remote\docker

# 構建所有服務的映像檔
docker-compose build

# 強制重新構建（不使用快取）
docker-compose build --no-cache
```

---

## 啟動容器

### 使用 Docker Compose（推薦）

```bash
cd D:\develop\PinionCore.Remote\docker

# 前景啟動（查看即時日誌）
docker-compose up

# 背景啟動（detached mode）
docker-compose up -d

# 只啟動特定服務
docker-compose up -d router
docker-compose up -d chat-server-1
```

### 手動啟動個別容器

#### 啟動 Router

```bash
docker run -d \
  --name gateway-router \
  -p 8001:8001 \
  -p 8002:8002 \
  -p 8003:8003 \
  --network gateway-network \
  gateway-router:latest \
  --agent-tcp-port=8001 \
  --agent-web-port=8002 \
  --registry-tcp-port=8003
```

#### 啟動 Chat Server

```bash
# 啟動 Chat Server 1
docker run -d \
  --name chat-server-1 \
  --network gateway-network \
  chat-server:latest \
  --router-host=router \
  --router-port=8003 \
  --group=1

# 啟動 Chat Server 2
docker run -d \
  --name chat-server-2 \
  --network gateway-network \
  chat-server:latest \
  --router-host=router \
  --router-port=8003 \
  --group=1
```

---

## 管理容器

### 查看狀態

```bash
# 查看所有服務狀態
docker-compose ps

# 查看特定容器詳情
docker inspect gateway-router

# 查看資源使用情況
docker stats
```

### 停止容器

```bash
# 優雅停止所有服務（30 秒超時）
docker-compose stop

# 立即停止所有服務
docker-compose kill

# 停止特定服務
docker-compose stop router
```

### 重啟容器

```bash
# 重啟所有服務
docker-compose restart

# 重啟特定服務
docker-compose restart chat-server-1
```

### 移除容器

```bash
# 停止並移除所有容器
docker-compose down

# 停止並移除容器、網路、映像檔
docker-compose down --rmi all

# 停止並移除容器、網路、資料卷
docker-compose down -v
```

---

## 配置參數

### Router 環境變數

| 變數名 | 預設值 | 說明 |
|-------|--------|------|
| `AGENT_TCP_PORT` | 8001 | Agent TCP 連接端口 |
| `AGENT_WEB_PORT` | 8002 | Agent WebSocket 連接端口 |
| `REGISTRY_TCP_PORT` | 8003 | Registry TCP 連接端口 |

**範例**:
```yaml
services:
  router:
    environment:
      - AGENT_TCP_PORT=9001
      - AGENT_WEB_PORT=9002
      - REGISTRY_TCP_PORT=9003
    command:
      - --agent-tcp-port=9001
      - --agent-web-port=9002
      - --registry-tcp-port=9003
```

### Chat Server 環境變數

| 變數名 | 預設值 | 說明 |
|-------|--------|------|
| `ROUTER_HOST` | - | Router 主機名稱或 IP |
| `ROUTER_PORT` | - | Router Registry 端口 |
| `GROUP` | 1 | 服務群組 ID |
| `TCP_PORT` | 23916 | 直連 TCP 端口（可選） |
| `WEB_PORT` | 23917 | 直連 WebSocket 端口（可選） |

**Gateway 模式範例**:
```yaml
services:
  chat-server-1:
    environment:
      - ROUTER_HOST=router
      - ROUTER_PORT=8003
      - GROUP=1
    command:
      - --router-host=router
      - --router-port=8003
      - --group=1
```

**最大相容性模式範例**:
```yaml
services:
  chat-server-1:
    ports:
      - "23916:23916"
      - "23917:23917"
    environment:
      - TCP_PORT=23916
      - WEB_PORT=23917
      - ROUTER_HOST=router
      - ROUTER_PORT=8003
      - GROUP=1
    command:
      - --tcp-port=23916
      - --web-port=23917
      - --router-host=router
      - --router-port=8003
      - --group=1
```

### 網路配置

預設網路配置:
```yaml
networks:
  gateway-network:
    driver: bridge
    ipam:
      driver: default
      config:
        - subnet: 172.20.0.0/16
          gateway: 172.20.0.1
```

**自訂子網路**:
```yaml
networks:
  gateway-network:
    driver: bridge
    ipam:
      config:
        - subnet: 10.10.0.0/24
          gateway: 10.10.0.1
```

---

## 日誌查看

### Docker Compose 日誌

```bash
# 查看所有服務日誌
docker-compose logs

# 即時追蹤日誌（follow mode）
docker-compose logs -f

# 查看特定服務日誌
docker-compose logs router
docker-compose logs chat-server-1

# 查看最後 N 行日誌
docker-compose logs --tail=100 router

# 顯示時間戳
docker-compose logs -t -f
```

### 個別容器日誌

```bash
# 查看 Router 日誌
docker logs gateway-router

# 即時追蹤
docker logs -f gateway-router

# 查看最後 50 行
docker logs --tail=50 gateway-router

# 顯示時間戳
docker logs -t gateway-router
```

### 日誌檔案位置

容器內日誌檔案位置:
- Router: `/app/RouterConsole_yyyy_MM_dd_HH_mm_ss.log`
- Chat Server: `/app/ChatServer_yyyy_MM_dd_HH_mm_ss.log`

**匯出日誌檔案**:
```bash
# 複製 Router 日誌到本地
docker cp gateway-router:/app/RouterConsole_2025_10_25_12_00_00.log ./logs/

# 列出容器內的日誌檔案
docker exec gateway-router ls -l /app/*.log
```

---

## 常見問題排解

### 1. 端口衝突

**症狀**: 啟動失敗，錯誤訊息包含 "bind: address already in use"

**解決方法**:
```bash
# 檢查端口占用情況
netstat -ano | findstr "8001"
netstat -ano | findstr "8002"
netstat -ano | findstr "8003"

# 方案 1: 停止占用端口的進程
taskkill /PID <PID> /F

# 方案 2: 修改 docker-compose.yml 使用不同端口
ports:
  - "9001:8001"  # 主機使用 9001, 容器使用 8001
```

### 2. 容器無法啟動

**症狀**: 容器狀態顯示 "Exited" 或 "Restarting"

**診斷步驟**:
```bash
# 查看容器狀態
docker-compose ps

# 查看容器日誌
docker-compose logs router

# 檢查容器詳細資訊
docker inspect gateway-router

# 進入容器內部檢查
docker exec -it gateway-router /bin/bash
```

### 3. Chat Server 無法連接到 Router

**症狀**: Chat Server 日誌顯示連線失敗或超時

**檢查步驟**:
```bash
# 確認 Router 容器正在運行
docker-compose ps router

# 確認網路連接
docker exec chat-server-1 ping -c 3 router

# 檢查 Router 是否監聽 Registry 端口
docker exec gateway-router netstat -an | grep 8003

# 檢查 Chat Server 日誌
docker-compose logs chat-server-1
```

**解決方法**:
```bash
# 重啟 Chat Server
docker-compose restart chat-server-1

# 確認環境變數正確
docker inspect chat-server-1 | grep -A 5 Env
```

### 4. 健康檢查失敗

**症狀**: Router 容器狀態顯示 "unhealthy"

**診斷步驟**:
```bash
# 查看健康檢查日誌
docker inspect gateway-router | grep -A 10 Health

# 手動執行健康檢查命令
docker exec gateway-router netstat -an | grep 8001
```

**解決方法**:
```yaml
# 調整健康檢查配置
healthcheck:
  interval: 30s      # 增加檢查間隔
  timeout: 10s       # 增加超時時間
  retries: 5         # 增加重試次數
  start_period: 20s  # 增加啟動寬限期
```

### 5. 構建映像檔失敗

**症狀**: `docker build` 或 `docker-compose build` 失敗

**常見原因與解決**:
```bash
# 原因 1: 磁碟空間不足
docker system df  # 檢查磁碟使用情況
docker system prune -a  # 清理未使用的映像檔與容器

# 原因 2: 依賴套件下載失敗
docker-compose build --no-cache  # 清除快取重新構建

# 原因 3: 專案路徑錯誤
# 確認 Dockerfile 中的 COPY 路徑正確

# 原因 4: NuGet 套件還原失敗
# 檢查網路連接與 NuGet 來源
```

### 6. 容器優雅關閉超時

**症狀**: 停止容器時等待時間過長

**說明**: `stop_grace_period` 設定為 30 秒,確保應用程式有足夠時間完成優雅關閉

**調整方法**:
```yaml
services:
  router:
    stop_grace_period: 60s  # 增加到 60 秒
```

### 7. 日誌檔案過大

**症狀**: 容器日誌佔用大量磁碟空間

**解決方法**:
```yaml
# 調整日誌配置
logging:
  driver: "json-file"
  options:
    max-size: "5m"   # 減少單個日誌檔案大小
    max-file: "2"    # 減少保留的日誌檔案數量
```

**手動清理日誌**:
```bash
# 查看日誌檔案大小
docker inspect gateway-router | grep LogPath

# 截斷日誌檔案（需要 root 權限）
truncate -s 0 $(docker inspect --format='{{.LogPath}}' gateway-router)
```

---

## 效能監控

### 資源使用情況

```bash
# 即時監控
docker stats

# 查看特定容器
docker stats gateway-router chat-server-1

# 顯示所有容器（包括停止的）
docker stats --all
```

### 網路流量

```bash
# 安裝並使用 ctop (容器監控工具)
docker run --rm -ti \
  --name=ctop \
  --volume /var/run/docker.sock:/var/run/docker.sock:ro \
  quay.io/vektorlab/ctop:latest
```

---

## 進階使用

### 擴展 Chat Server 數量

```bash
# 動態增加 Chat Server 實例
docker-compose up -d --scale chat-server-1=3

# 或在 docker-compose.yml 中配置
services:
  chat-server-1:
    deploy:
      replicas: 3
```

### 使用外部 Router

```yaml
services:
  chat-server-1:
    environment:
      - ROUTER_HOST=192.168.1.100  # 外部 Router IP
      - ROUTER_PORT=8003
```

### 持久化日誌

```yaml
services:
  router:
    volumes:
      - ./logs/router:/app/logs  # 將日誌掛載到主機
```

---

## 參考資源

- [Docker 官方文檔](https://docs.docker.com/)
- [Docker Compose 文檔](https://docs.docker.com/compose/)
- [PinionCore.Remote 專案](https://github.com/jiowchern/PinionCore.Remote)
- [專案規格文件](../specs/002-gateway-router-console/spec.md)

---

**最後更新**: 2025-10-25
**版本**: 1.0.0
