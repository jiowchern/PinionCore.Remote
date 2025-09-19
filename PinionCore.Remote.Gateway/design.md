
# Gateway Architecture

在真實的環境中為了避免曝光遊戲伺服器的 IP 位址, 會在遊戲伺服器前端加上一層 Gateway 來處理 Client 的連線請求,
以 PinionCore Remote 來說從原先的建立 Agent 直接連線到 GameServer, 變成建立 GatewaySession 連線到 Gateway 再由 Gateway 分配已經註冊的遊戲服務給 GatewaySession, GatewaySession 再建立對應的 Agent 與 GameServer 互動。

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

目標修改後的架構, Client 建立 GatewaySession 連線到 Gateway, Gateway 把已經註冊的服務分配給 GatewaySession, GatewaySession 再建立對應的 Agent 與 GameServer 互動。
```mermaid
---
config:
  layout: elk  
---
flowchart LR
 subgraph GatewayHost["GatewayHost"]
        SessionDispatcher@{ label: "<b>SessionDispatcher<br></b>為 GatewaySession 分配來自<b></b><span style=\"font-weight:\">ServiceRegistry 的 Service</span>" }
        ServiceRegistry@{ label: "<b>ServiceRegistry</b><br>1.接收<span style=\"--tw-scale-x:\">SessionListener的註冊請求, 建立 Service</span><br>2.接收<span style=\"--tw-scale-x:\">SessionDispatcher 的通知為User 分配 Service</span><br>" }
  end
 subgraph ClientSession["ClientSession"]
        GatewaySession["<b>GatewaySession</b><br>接收 SessionDispatcher 通知建立對應的 Agents"]
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
        SessionListener@{ label: "<b>SessionListener</b><br>繼承 Soul.IListenable<br>接收<span style=\"font-weight:\">ServiceRegistry通知, 建立User</span>" }
  end
    GatewaySession -. connect .-> SessionDispatcher
    GatewaySession -- create --> Agent
    SessionDispatcher -- invoke --> ServiceRegistry
    SessionListener -- event --> UserProvider
    SessionListener -. connect .-> ServiceRegistry
    SessionDispatcher@{ shape: rect}
    ServiceRegistry@{ shape: rect}
    SessionListener@{ shape: rect}
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

    note for GatewaySession "從 GatewayHost 接收 SessionDispatcher 通知"
    class GatewaySession {
	    -map[uint]IAgent _Agents
	    +Create(uint group) Ghost.IAgent
	    +Destroy(uint group) bool
	    +OnMessage(uint group, byte[] payload) ;
    }
    class UserSession {
	    -IStreamable _Client
	    +PackageReader Reader
	    +PackageSender Sender
	    +HashSet Groups
    }
    note for SessionDispatcher "為 GatewaySession 分配來自 ServiceRegistry 的 Service"
    class SessionDispatcher {
	    +Register(IStreamable) UserSession
	    +Unregister(IStreamable) UserSession
    }
    class ServiceSession {
	    -IStreamable _Service
	    +uint Group
	    +Bind(UserSession)
	    +Unbind(UserSession)
    }

    note for ServiceRegistry "1.接收 SessionListener 的註冊請求, 建立 Service 2.接收 SessionDispatcher 的通知為 User 分配 Service"
    class ServiceRegistry {
	    -ServiceSession[] _Services
	    -UserSession[] _Users
	    +Join(UserSession)
	    +Leave(UserSession)
	    +Register(uint group,IStreamable) bool
	    +Unregister(uint group,IStreamable) bool
    }

    note for SessionListener "繼承 Soul.IListenable 接收 ServiceRegistry 通知, 建立 User IStreamable"
    class SessionListener {
        -IStreamable[] _Users
	    -IStreamable _Stream
    }
    class ClientSession {
        -GatewaySession _AgentSession
    }

    class GatewayHost {
        -SessionDispatcher _Dispatcher
        -ServiceRegistry _Registry
    }

    class GameServer {
        -UserProvider _Users
        -SessionListener _Listener
    }
	<<interface>> IAgent
	<<interface>> IStreamable
	<<interface>> IListenable
	<<interface>> IAgentProvider
    IAgentProvider <|-- GatewaySession
    IStreamable <|-- ServiceSession
    IListenable <|-- SessionListener

```
## 時序圖

### GatewaySession 連線流程
```mermaid
sequenceDiagram
    participant GatewaySession 
    participant SessionDispatcher
    participant ServiceRegistry
    participant SessionListener

    GatewaySession ->> SessionDispatcher: Connect(IStreamable client)
    SessionDispatcher ->> SessionDispatcher : Create UserSession
    SessionDispatcher ->> ServiceRegistry: Join(UserSession user)
    ServiceRegistry ->> ServiceRegistry: Assign ServiceSessions to UserSession
    ServiceRegistry ->> SessionListener: Notify new User(IStreamable user)

    SessionListener ->> SessionListener: Create User IStreamable
    SessionListener  ->> UserProvider: event IListenable.StreamEnterEvent
```

### GatewaySession 斷線流程
```mermaid
sequenceDiagram
    participant GatewaySession 
    participant SessionDispatcher
    participant ServiceRegistry
    participant SessionListener
    GatewaySession ->> SessionDispatcher: Disconnect(IStreamable client)
    SessionDispatcher ->> ServiceRegistry: Leave(UserSession user)
    ServiceRegistry ->> ServiceRegistry: Unbind ServiceSessions from UserSession
    ServiceRegistry ->> SessionListener: Notify lost User(IStreamable user)
    SessionListener ->> SessionListener: Remove User IStreamable
    SessionListener  ->> UserProvider: event IListenable.StreamLeaveEvent
```

### GatewaySession 建立 Agent 流程
```mermaid
sequenceDiagram    
    UserProvider ->> SessionListener: IStreamable.Send (any package)
    SessionListener ->> ServiceRegistry: sessionid + PinionCore.Remote.Packages.PackageProtocolSubmit
    ServiceRegistry -> ServiceSession : find by group
    ServiceSession ->> UserSession: find by sessionid
    UserSession ->> GatewaySession: send group + payload
    GatewaySession -->> Agent : if group not exist create
```
### 
