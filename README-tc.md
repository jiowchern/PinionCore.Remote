


# PinionCore Remote（繁體中文）
[![Maintainability](https://api.codeclimate.com/v1/badges/89c3a646f9daff42a38e/maintainability)](https://codeclimate.com/github/jiowchern/PinionCore.Remote/maintainability)
[![Build](https://github.com/jiowchern/PinionCore.Remote/actions/workflows/dotnet-desktop.yml/badge.svg?branch=master)](https://github.com/jiowchern/PinionCore.Remote/actions/workflows/dotnet-desktop.yml)
[![Coverage Status](https://coveralls.io/repos/github/jiowchern/PinionCore.Remote/badge.svg?branch=master)](https://coveralls.io/github/jiowchern/PinionCore.Remote?branch=master)
![commit last date](https://img.shields.io/github/last-commit/jiowchern/PinionCore.Remote)

## 簡介
PinionCore Remote 是以 C# 開發的伺服器－客戶端通訊框架，能在 Unity 與任何支援 .NET Standard 2.0 的環境中運作。它透過「以介面進行互動」的物件導向方式來簡化網路通訊，降低協議維護成本並提升可讀性與可維護性。

框架支援 IL2CPP 與 AOT，涵蓋包含 Unity WebGL 在內的多種平台。預設提供 TCP 與序列化機制，也可依需求自訂。除方法外，亦支援事件、屬性與 notifier，讓你能建構完整的網路應用。另提供 Standalone 模式，可在無網路下模擬伺服器與客戶端互動，加速開發與除錯。

## 功能
透過介面在伺服器與客戶端之間傳遞物件，降低協議維護成本並強化可讀性。

![plantUML](http://www.plantuml.com/plantuml/svg/ZP31JiCm38RlUGeVGMXzWAcg9kq0ko4g7Y1aVql1IIR7GqAZxqvRLGdiD5zgsVw_ViekgHKzUpOdwpvj3tgMgD55fhf-WLCRUaRJN0nDDGI5TDQ13ey2A8IcnLeFhVr-0dEykrzcencDoTWMyWNv3rt3ZcrAT1EmyFOy8EYrPC6rqMC_TuLtwGRmSIpk_VejzBpQR9g2s6xpPJweVwegEvCn8Ig8qId5himNyi6V67wspMc3SAGviWPbwD_dvDK_Yzrh0iMt3pYbJgAdj3ndzOUpczgpvry0)

## 支援
- 支援 IL2CPP 與 AOT
- 相容 .NET Standard 2.0 以上
- 內建 TCP 連線與序列化，可自行擴充
- 支援 Unity WebGL（伺服器端 WebSocket，客戶端需自訂）

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

3) 新增 `ProtocolCreater.cs` 以產生 `IProtocol`

```csharp
namespace Protocol
{
	public static partial class ProtocolCreater
	{
		public static PinionCore.Remote.IProtocol Create()
		{
			PinionCore.Remote.IProtocol protocol = null;
			_Create(ref protocol);
			return protocol;
		}

		[PinionCore.Remote.Protocol.Creater]
		static partial void _Create(ref PinionCore.Remote.IProtocol protocol);
	}
}
```

注意：被標記為 `PinionCore.Remote.Protocol.Creater` 的方法需為 `static partial void Method(ref PinionCore.Remote.IProtocol)`，否則無法編譯通過。

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
		var protocol = Protocol.ProtocolCreater.Create();
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
		var protocol = Protocol.ProtocolCreater.Create();
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
var protocol = Protocol.ProtocolCreater.Create();
var entry = new Entry();
var service = PinionCore.Remote.Standalone.Provider.CreateService(entry , protocol);
var agent = service.Create();
// 依循與 Client 類似的輪詢/事件流程
```

---

## 自訂連線（Custom Connection）
客戶端以 `CreateAgent(protocol, IStreamable)` 建立，並自行實作 `IStreamable`：

```csharp
var protocol = Protocol.ProtocolCreater.Create();
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

