


# PinionCore Remote
[![Maintainability](https://api.codeclimate.com/v1/badges/89c3a646f9daff42a38e/maintainability)](https://codeclimate.com/github/jiowchern/PinionCore.Remote/maintainability)
[![Build](https://github.com/jiowchern/PinionCore.Remote/actions/workflows/dotnet-desktop.yml/badge.svg?branch=master)](https://github.com/jiowchern/PinionCore.Remote/actions/workflows/dotnet-desktop.yml)
[![Coverage Status](https://coveralls.io/repos/github/jiowchern/PinionCore.Remote/badge.svg?branch=master)](https://coveralls.io/github/jiowchern/PinionCore.Remote?branch=master)
![commit last date](https://img.shields.io/github/last-commit/jiowchern/PinionCore.Remote)

## 簡介
PinionCore Remote 是以 C# 開發的的網路通訊框架，採用物件導向的介面定義方式，讓伺服器與客戶端能夠輕鬆地透過介面進行通訊。

## 功能
透過介面在伺服器與客戶端之間傳遞物件，降低協議維護成本並強化可讀性。
![plantUML](http://www.plantuml.com/plantuml/svg/ZP31JiCm38RlUGeVGMXzWAcg9kq0ko4g7Y1aVql1IIR7GqAZxqvRLGdiD5zgsVw_ViekgHKzUpOdwpvj3tgMgD55fhf-WLCRUaRJN0nDDGI5TDQ13ey2A8IcnLeFhVr-0dEykrzcencDoTWMyWNv3rt3ZcrAT1EmyFOy8EYrPC6rqMC_TuLtwGRmSIpk_VejzBpQR9g2s6xpPJweVwegEvCn8Ig8qId5himNyi6V67wspMc3SAGviWPbwD_dvDK_Yzrh0iMt3pYbJgAdj3ndzOUpczgpvry0)

## 支援
- 支援 IL2CPP 與 AOT
- 相容 .NET Standard 2.1 以上
- 內建 TCP 連線與序列化，可自行擴充
- 支援 Unity WebGL（伺服器端 WebSocket，客戶端需自訂）

## 特色
### 1.介面導向通訊
PinionCore Remote 採用介面導向的設計，讓開發者能夠以物件導向的方式定義伺服器與客戶端之間的通訊協議。透過定義介面，開發者可以清晰地描述通訊行為，並且讓伺服器與客戶端能夠輕鬆地實作這些介面。  

```csharp
// 前端直接的方法呼叫遠端方法
var val = fromserverInstance.Method1();
val.OnValue += result => 
{
	// 處理回傳結果
};
```
### 2.輕量級
PinionCore Remote 採用輕量級設計，核心程式庫僅包含必要的通訊功能，並且提供擴充點讓開發者能夠根據需求自訂連線與序列化方式。這使得 PinionCore Remote 能夠在各種環境中運行，包括資源有限的裝置與高效能伺服器。

### 3.可控的生命週期
PinionCore Remote 提供了明確的生命週期管理機制，讓開發者能夠控制物件的建立與銷毀。透過 `IBinder`的 Bind 與 Unbind 方法，開發者可以在適當的時機點註冊與取消註冊介面實作，確保資源的有效利用與權限控管。
```csharp
var player = new Player();
binder.Bind<IPlayer>(player); // 綁定 IPlayer 介面
if player.IsDeveloper() {
	binder.Bind<IDeveloper>(player); // 檢查如果這個 玩家是開發者，才綁定 IDeveloper 介面, 簡易的權限控管機制
}
```
### 4.巢狀介面支援
PinionCore Remote 支援巢狀介面的定義與使用，讓開發者能夠在介面中包含其他介面，進一步提升通訊協議的結構化與可讀性。這使得複雜的通訊行為能夠被清晰地描述與實作。
**以RPG遊戲舉例**
```csharp
// 每個npc或玩家都擁有一些基本屬性比如名字等級
interface IActor { 
	Property<string> Name {get};
	Property<int> Level {get};
}

// 玩家獨有的介面，繼承自IActor
interface IPlayer : IActor {
	
    Notifier<IActor> Actors; // (重點)玩家看的到的其他角色列表 
	Property<int> Gold {get; } // 玩家金幣 只有自己才看的到
	Value<Path> Move(Postion pos) ;// 玩家移動方法
	event System.Action<Postion> StopEvent; // 玩家停止通知
}
```
### 5.即時通知機制
PinionCore Remote 提供即時通知機制，讓伺服器能夠主動向客戶端推送事件與狀態變更。透過 Notifier 與 Event 的設計，開發者可以輕鬆地實現即時通訊功能，提升使用者體驗。
```csharp
// 伺服器端
class GameServer : IGameServer {
	Notifier<IPlayer> _Players = new Notifier<IPlayer>();
	
	void IBinderProvider.RegisterClientBinder(IBinder binder) {
		binder.Bind<IGameServer>(this);
	}
	
	// 當有新玩家加入遊戲時
	void OnPlayerJoin(IPlayer player) {
		_Players.Add(player); // 通知所有客戶端有新玩家加入
	}
}
// 客戶端
class GameClient {
	public GameClient(IAgent agent) {
		agent.QueryNotifier<IGameServer>().Supply += _OnGameServerSupplied;
	}
	
	void _OnGameServerSupplied(IGameServer gameServer) {
		gameServer.Players.Supply += _OnPlayerAdded;
	}
	
	void _OnPlayerAdded(IPlayer player) {
		// 處理新玩家加入的邏輯
	}
}
```
### 6. 響應式方法支援
PinionCore Remote 支援響應式方法，讓開發者能夠定義非同步且可監聽的遠端方法呼叫。  
**範例**
```csharp
// 前端代碼 (概念碼)
var obs = from path in player.Move(position) // 調用遠端移動
		  from _ in avatar.PlayMove(path) // 將返回的路徑交給前端播放
		  from pos in player.StopEvent() // 收到服務端停止角色移動
		  from _ in avatar.Stop(pos) // 調用停止
		  select new {path , pos} 
```
### 7. 閘道服務器支援
在真實環境下如果遊戲沒有即時反應的需求可以使用閘道服務器減少機器曝光於公網上
詳細參閱 Gateway 章節



## 使用方式
1. 定義介面 `IGreeter`

```csharp
namespace Protocol
{
	public struct HelloRequest
	{
		public string Name;
	}
	public struct HelloReply
	{
		public string Message;
	}
	public interface IGreeter
	{
		PinionCore.Remote.Value<HelloReply> SayHello(HelloRequest request);
	}
}
```

2. 伺服器實作 `IGreeter`

```csharp
namespace Server
{	
	class Greeter : IGreeter
	{
		PinionCore.Remote.Value<HelloReply> SayHello(HelloRequest request)
		{
			return new HelloReply() { Message = $"Hello {request.Name}." };
		}
	}
}
```

3. 伺服器用 `IBinder.Bind` 提供 `IGreeter` 給客戶端

```csharp
namespace Server
{
	public class Entry	
	{
		readonly Greeter _Greeter;
		readonly PinionCore.Remote.IBinder _Binder;
		readonly PinionCore.Remote.ISoul _GreeterSoul;
		public Entry(PinionCore.Remote.IBinder binder)
		{
			_Greeter = new Greeter();
			_Binder = binder;
			_GreeterSoul = binder.Bind<IGreeter>(_Greeter);
		}
		public void Dispose()
		{
			_Binder.Unbind(_GreeterSoul);
		}
	}
}
```

4. 客戶端以 `IAgent.QueryNotifier` 取得 `IGreeter`

```csharp
namespace Client
{
	class Entry
	{
		public Entry(PinionCore.Remote.IAgent agent)
		{
			agent.QueryNotifier<IGreeter>().Supply += _AddGreeter;
			agent.QueryNotifier<IGreeter>().Unsupply += _RemoveGreeter;
		}
		async void  _AddGreeter(IGreeter greeter)
		{
			var reply = await greeter.SayHello(new HelloRequest() {Name = "my"});
		}
		void _RemoveGreeter(IGreeter greeter)
		{
			// 伺服器已取消供應 greeter
		}
	}
}
```

完成上述步驟後，伺服器與客戶端即可透過介面進行物件導向的通訊。

### 規格
- 介面型態：
  - 方法（Method）
  - 事件（Event）
  - 屬性（Property）
  - Notifier
  詳見 `document/communications-*.md`。

**串流方法（Streamable Method）**  
當介面定義方法簽名為 `PinionCore.Remote.IAwaitableSource<int> AnyName(byte[] buffer, int offset, int count)` 時，Source Generator 會自動將其視為串流呼叫。傳送至伺服器的資料僅限於 `buffer` 由 `offset` 與 `count` 所描述的區段，伺服器回覆的處理長度與資料會原地寫回 `offset` 起始的位置。適用於需要雙向資料流、又不希望每次都複製整個緩衝區的情境。

- 序列化：可序列化型別與說明見 `PinionCore.Serialization/README.md`。

---

## 快速開始（Getting Started）
這是伺服器－客戶端框架，建議建立三個專案：`Protocol`、`Server`、`Client`。

### 需求
- Visual Studio 2022 17.0.5 以上
- .NET SDK 5 以上

### Protocol 專案
建立共用介面專案 `Protocol.csproj`：

```powershell
Sample/Protocol>dotnet new classlib
```

1) 參考與套件

```xml
<ItemGroup>
	<PackageReference Include="PinionCore.Remote" Version="0.1.13.15" />
	<PackageReference Include="PinionCore.Serialization" Version="0.1.13.12" />
	<PackageReference Include="PinionCore.Remote.Tools.Protocol.Sources" Version="0.0.1.25">
		<PrivateAssets>all</PrivateAssets>
		<IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
	</PackageReference>
</ItemGroup>
```

2) 新增介面 `IGreeter.cs`

```csharp
namespace Protocol
{
	public interface IGreeter
	{
		PinionCore.Remote.Value<string> SayHello(string request);
	}
}
```

3) 新增 `ProtocolCreator.cs` 以產生 `IProtocol`

```csharp
namespace Protocol
{
	public static partial class ProtocolCreator
	{
		public static PinionCore.Remote.IProtocol Create()
		{
			PinionCore.Remote.IProtocol protocol = null;
			_Create(ref protocol);
			return protocol;
		}

		[PinionCore.Remote.Protocol.Creator]
		static partial void _Create(ref PinionCore.Remote.IProtocol protocol);
	}
}
```

注意：被標記為 `PinionCore.Remote.Protocol.Creator` 的方法需為 `static partial void Method(ref PinionCore.Remote.IProtocol)`，否則無法編譯通過。

### Server 專案

```powershell
Sample/Server>dotnet new console
```

1) 參考與套件

```xml
<ItemGroup>
	<PackageReference Include="PinionCore.Remote.Server" Version="0.1.13.13" />
	<ProjectReference Include="..\Protocol\Protocol.csproj" />
</ItemGroup>
```

2) 實作 `IGreeter`

```csharp
namespace Server
{
	public class Greeter : Protocol.IGreeter
	{
		PinionCore.Remote.Value<string> SayHello(string request)
		{
			return $"echo:{request}";
		}
	}
}
```

3) 建立入口點 `Entry` 啟動環境（實作 `PinionCore.Remote.IEntry`）

```csharp
namespace Server
{
	public class Entry : PinionCore.Remote.IEntry
	{
		void IBinderProvider.RegisterClientBinder(IBinder binder)
		{
			binder.Binder<IGreeter>(new Greeter());
		}

		void IBinderProvider.UnregisterClientBinder(IBinder binder)
		{
			// 客戶端斷線時
		}

		void IEntry.Update()
		{
			// 每幀更新
		}
	}
}
```

4) 建立 TCP 服務

```csharp
namespace Server
{
	static void Main(string[] args)
	{
		var protocol = Protocol.ProtocolCreator.Create();
		var entry = new Entry();
		var set = PinionCore.Remote.Server.Provider.CreateTcpService(entry, protocol);
		int yourPort = 0;
		set.Listener.Bind(yourPort);

		// 關閉服務
		set.Listener.Close();
		set.Service.Dispose();
	}
}
```

### Client 專案

```powershell
Sample/Client>dotnet new console
```

1) 參考與套件

```xml
<ItemGroup>
	<PackageReference Include="PinionCore.Remote.Client" Version="0.1.13.12" />
	<ProjectReference Include="..\Protocol\Protocol.csproj" />
</ItemGroup>
```

2) 建立 TCP 客戶端

```csharp
namespace Client
{
	static async Task Main(string[] args)
	{
		var protocol = Protocol.ProtocolCreator.Create();
		var set = PinionCore.Remote.Client.Provider.CreateTcpAgent(protocol);

		bool stop = false;
		var task = System.Threading.Tasks.Task.Run(() => 
		{
			while (!stop)
			{
				set.Agent.HandleMessages();
				set.Agent.HandlePackets();
			}
		});

		// 開始連線
		EndPoint yourEndPoint = null;
		var peer = await set.Connector.Connect(yourEndPoint);
		set.Agent.Enable(peer);

		// 當伺服器供應 IGreeter
		set.Agent.QueryNotifier<Protocol.IGreeter>().Supply += greeter => 
		{
			greeter.SayHello("hello");
		};

		// 當伺服器取消供應 IGreeter
		set.Agent.QueryNotifier<Protocol.IGreeter>().Unsupply += greeter => { };

		// 關閉
		stop = true;
		task.Wait();
		set.Connector.Disconnect();
		set.Agent.Disable();
	}
}
```

### Standalone（單機）
無需網路即可模擬 Server/Client：

```csharp
var protocol = Protocol.ProtocolCreator.Create();
var entry = new Entry();
var service = PinionCore.Remote.Standalone.Provider.CreateService(entry , protocol);
var agent = service.Create();
// 依循與 Client 類似的輪詢/事件流程
```

---

## 自訂連線（Custom Connection）
客戶端以 `CreateAgent(protocol, IStreamable)` 建立，並自行實作 `IStreamable`：

```csharp
var protocol = Protocol.ProtocolCreator.Create();
IStreamable stream = null; // 自行實作 IStreamable
var service = PinionCore.Remote.Client.CreateAgent(protocol, stream);
```

```csharp
namespace PinionCore.Network
{
    public interface IStreamable
    {
        IWaitableValue<int> Receive(byte[] buffer, int offset, int count);
        IWaitableValue<int> Send(byte[] buffer, int offset, int count);
    }
}
```

伺服器以 `CreateService(entry, protocol, IListenable)` 建立，並自行實作 `IListenable`。

```csharp
namespace PinionCore.Remote.Soul
{
    public interface IListenable
    {
        event System.Action<Network.IStreamable> StreamableEnterEvent;
        event System.Action<Network.IStreamable> StreamableLeaveEvent;
    }
}
```

## 自訂序列化（Custom Serialization）
實作 `ISerializable`，並在 Server/Client 建立時帶入：

```csharp
namespace PinionCore.Remote
{
    public interface ISerializable
    {
        PinionCore.Memorys.Buffer Serialize(System.Type type, object instance);
        object Deserialize(System.Type type, PinionCore.Memorys.Buffer buffer);
    }
}
```

```csharp
// Server 端
var service = PinionCore.Remote.Server.CreateTcpService(entry, protocol, yourSerializer);

// Client 端
var service = PinionCore.Remote.Client.CreateTcpAgent(protocol, yourSerializer);
```

需要被序列化的型別可參考 `IProtocol.SerializeTypes`。

```csharp
namespace PinionCore.Remote
{
	public interface IProtocol
	{
		System.Type[] SerializeTypes { get; }
		System.Reflection.Assembly Base { get; }
		EventProvider GetEventProvider();
		InterfaceProvider GetInterfaceProvider();
		MemberMap GetMemberMap();
		byte[] VersionCode { get; }
	}
}
```

### Gateway 模組
**PinionCore.Remote.Gateway** 模組提供分散式服務閘道架構，實現客戶端與多個後端服務之間的智慧路由與連線管理。主要功能包括：

- **多服務架構**：允許客戶端透過單一 Router 進入點連接多個後端服務
- **智慧路由**：支援可自訂的路由策略（預設：輪詢），在服務實例之間分配客戶端連線
- **群組化組織**：服務可組織為群組，群組內自動進行負載平衡
- **協議版本支援**：支援多個協議版本同時執行，讓不同版本的客戶端與服務能在同一系統中無縫共存
- **透明代理**：客戶端透過統一介面與遠端服務互動，無需管理個別連線
- **單機模式支援**：包含單機測試模式，開發時無需網路基礎設施

詳細文件請參閱 [PinionCore.Remote.Gateway/README.md](PinionCore.Remote.Gateway/README.md)。

