# Gateway Architecture

在真實的環境中為了避免曝光遊戲伺服器的 IP 位址, 會在遊戲伺服器前端加上一層 Gateway 來處理 Client 的連線請求,
以 PinionCore Remote 來說從原先的建立 Agent 直接連線到 GameServer, 變成建立 GatewayClientSession 連線到 Gateway 再由 Gateway 分配已經註冊的遊戲服務給 GatewayClientSession, GatewayClientSession 再建立對應的 Agent 與 GameServer 互動。

## 架構修改

這是原本的架構, Client 直接建立 Agent 連線到 GameServer
```mermaid
---
config:
  
---
flowchart LR
    subgraph GameClient["GameClient"]
        Agent1["Ghost.IAgent"]
        
    end
    subgraph GameServer["GameServer"]
        Listener1["Soul.IListenable"]
    end

    Agent1 -- direct connect --> Listener1
```

目標修改後的架構, Client 建立 GatewayClientSession 連線到 Gateway, Gateway 把已經註冊的服務分配給 GatewayClientSession, GatewayClientSession 再建立對應的 Agent 與 GameServer 互動。
```mermaid
---
config:
  layout: elk  
---
flowchart LR
 subgraph GatewayHost["GatewayHost"]
        GatewaySessionCoordinator@{ label: "<b>GatewaySessionCoordinator<br></b>為 GatewayClientSession 分配來自<b></b><span style=\"font-weight:\">GatewayServiceRouter 的 Service</span>" }
        GatewayServiceRouter@{ label: "<b>GatewayServiceRouter</b><br>1.接收<span style=\"--tw-scale-x:\">GatewayUserListener的註冊請求, 建立 Service</span><br>2.接收<span style=\"--tw-scale-x:\">GatewaySessionCoordinator 的通知為User 分配 Service</span><br>" }
  end
 subgraph ClientSession["ClientSession"]
        GatewayClientSession["<b>GatewayClientSession</b><br>接收 GatewaySessionCoordinator 通知建立對應的 Agents"]
  end
 subgraph GameClient["GameClient"]
        Agent["Ghost.IAgent"]
        ClientSession
  end
 subgraph Gateway["Gateway"]
        GatewayHost
  end
 subgraph GameServers["GameServers"]
        GameServer["GameServer2"]
  end
 subgraph GameServer["GameServer"]
        UserProvider["<b>Soul.UserProvider</b>"]
        GatewayUserListener@{ label: "<b>GatewayUserListener</b><br>繼承 Soul.IListenable<br>接收<span style=\"font-weight:\">GatewayServiceRouter通知, 建立User</span>" }
  end
    GatewayClientSession -. connect .-> GatewaySessionCoordinator
    GatewayClientSession -- create --> Agent
    GatewaySessionCoordinator -- invoke --> GatewayServiceRouter
    GatewayUserListener -- event --> UserProvider
    GatewayUserListener -. connect .-> GatewayServiceRouter
    GatewaySessionCoordinator@{ shape: rect}
    GatewayServiceRouter@{ shape: rect}
    GatewayUserListener@{ shape: rect}
    style GameClient stroke-width:4px,stroke-dasharray: 5
    style Gateway stroke-width:4px,stroke-dasharray: 6
    style GameServers stroke-width:4px,stroke-dasharray: 7

```
## 類別圖
```mermaid
---
config:
  layout: dagre
---
classDiagram
direction RL
    note for IAgent "PinionCore.Remote.Ghost.IAgent"
    note for IListenable "PinionCore.Remote.Soul.IListenable"
    note for IStreamable "PinionCore.Network.IStreamable"
    class IAgent  {
        <<interface>>
    }
    
    class IListenable {
        <<interface>>
    }
    
    class IStreamable {
        <<interface>>
    }
    
    class IAgentProvider {
	    +CreateEvent System.Action~uint,IAgent~
	    +DestroyEvent System.Action~uint,IAgent~
    }

    note for GatewayClientSession "從 GatewayHost 接收 GatewaySessionCoordinator 通知"
    class GatewayClientSession {
	    -map[uint]IAgent _Agents
	    +Create(uint group) Ghost.IAgent
	    +Destroy(uint group) bool
	    +OnMessage(uint group, byte[] payload) ;
    }
    class UserSession {
	    -IStreamable _Client
	    +uint Id
	    +PackageReader Reader
	    +PackageSender Sender
	    +HashSet~uint~ ConnectedGroups
	    +SendToUser(uint group, byte[] payload)
    }
    note for GatewaySessionCoordinator "為 GatewayClientSession 分配來自 GatewayServiceRouter 的 Service"
    class GatewaySessionCoordinator {
	    +Register(IStreamable) UserSession
	    +Unregister(IStreamable) UserSession
    }
    class ServiceSession {
	    -IStreamable _Service
	    +uint Group
	    +Bind(UserSession)
	    +Unbind(UserSession)
	    +Send(ServiceRegistryPackage)
    }

    note for GatewayServiceRouter "1.接收 GatewayUserListener 的註冊請求 2.接收 GatewaySessionCoordinator 的通知為 User 分配 Service"
    class GatewayServiceRouter {
	    -Dictionary~uint,List~ServiceSession~~ _servicesByGroup
	    -Dictionary~uint,Dictionary~uint,ServiceSession~~ _serviceByUserAndGroup
	    -DataflowActor~Func~Task~~ _actor
	    +Join(UserSession)
	    +Leave(UserSession)
	    +Register(uint group,IStreamable)
	    +Unregister(uint group,IStreamable)
    }

    note for GatewayUserListener "繼承 Soul.IListenable 接收 GatewayServiceRouter 通知, 建立 User IStreamable"
    class GatewayUserListener {
        -IStreamable[] _Users
	    -IStreamable _Stream
    }
    class ClientSession {
        -GatewayClientSession _AgentSession
    }

    class GatewayHost {
        -GatewaySessionCoordinator _Dispatcher
        -GatewayServiceRouter _Registry
    }

    class GameServer {
        -UserProvider _Users
        -GatewayUserListener _Listener
    }
	<<interface>> IAgent
	<<interface>> IStreamable
	<<interface>> IListenable
	<<interface>> IAgentProvider
    IAgentProvider <|-- GatewayClientSession
    IStreamable <|-- ServiceSession
    IListenable <|-- GatewayUserListener

```

### GatewayServiceRouter 行為補充

- `UserSession` 由 `GatewaySessionCoordinator` 建立時即帶入唯一的 `Id`，與 `GatewayUserListener.User` 的 Id 對應，`GatewayServiceRouter` 僅依此 Id 維護狀態，不再重新分配。
- `Join(UserSession)` 會在單一 `DataflowActor` 執行緒上處理，針對目前註冊的每一個 group 選擇負載最低的 `ServiceSession` 並送出 `Join` 封包，封包中的 `UserId` 等於 `UserSession.Id`。
- User 與 Service 間的資料在傳遞時會在 payload 前方增加 4 bytes little-endian 的 group header；Gateway 端、`GatewayServiceRouter` 與 `UserSession` 都依據此 header 進行路由。
- `Register` 新服務時計算所有尚未在該 group 建立連線的 `UserSession`，立即補發 Join；`Unregister` 會將受影響的使用者重新指派到現存服務，若目前無可用服務則留下 TODO 以後處理。

## 時序圖

### GatewayClientSession 連線流程
```mermaid
sequenceDiagram
    participant GatewayClientSession 
    participant GatewaySessionCoordinator
    participant GatewayServiceRouter
    participant GatewayUserListener

    GatewayClientSession ->> GatewaySessionCoordinator: Connect(IStreamable client)
    GatewaySessionCoordinator ->> GatewaySessionCoordinator : Create UserSession (帶入 Id)
    GatewaySessionCoordinator ->> GatewayServiceRouter: Join(UserSession user)
    GatewayServiceRouter ->> GatewayServiceRouter: Assign ServiceSessions to UserSession (per group)
    GatewayServiceRouter ->> GatewayUserListener: Notify new User(IStreamable user)

    GatewayUserListener ->> GatewayUserListener: Create User IStreamable
    GatewayUserListener  ->> UserProvider: event IListenable.StreamEnterEvent
```

### GatewayClientSession 斷線流程
```mermaid
sequenceDiagram
    participant GatewayClientSession 
    participant GatewaySessionCoordinator
    participant GatewayServiceRouter
    participant GatewayUserListener
    GatewayClientSession ->> GatewaySessionCoordinator: Disconnect(IStreamable client)
    GatewaySessionCoordinator ->> GatewayServiceRouter: Leave(UserSession user)
    GatewayServiceRouter ->> GatewayServiceRouter: Unbind & reassign groups if any service remains
    GatewayServiceRouter ->> GatewayUserListener: Notify lost User(IStreamable user)
    GatewayUserListener ->> GatewayUserListener: Remove User IStreamable
    GatewayUserListener  ->> UserProvider: event IListenable.StreamLeaveEvent
```

### GatewayClientSession 建立 Agent 流程
```mermaid
sequenceDiagram    
    UserProvider ->> GatewayUserListener: IStreamable.Send (any package)
    GatewayUserListener ->> GatewayServiceRouter: sessionId + payload
    GatewayServiceRouter -> ServiceSession : route by group header
    ServiceSession ->> UserSession: locate by sessionId
    UserSession ->> GatewayClientSession: send [group header + payload]
    GatewayClientSession -->> Agent : if group not exist create
```
### 
