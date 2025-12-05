# 快速開始（Hello World）

[上一節：架構與模組](architecture.md) | [下一節：核心概念](core-concepts.md)

建議建立三個專案：**Protocol、Server、Client**。以下範例為簡化版本，完整範例見：

- `PinionCore.Samples.HelloWorld.Protocols`
- `PinionCore.Samples.HelloWorld.Server`
- `PinionCore.Samples.HelloWorld.Client`

## 環境需求

- .NET SDK 6 或以上
- Visual Studio 2022 / Rider / VS Code
- 若需 Unity，建議 Unity 2021 LTS 以上

## 1. Protocol 專案

建立 Class Library：

```bash
Sample/Protocol> dotnet new classlib
```

加入 NuGet 參考（版本請依實際發佈為準）：

```xml
<ItemGroup>
  <PackageReference Include="PinionCore.Remote" Version="0.2.0.0" />
  <PackageReference Include="PinionCore.Serialization" Version="0.2.0.0" />
  <PackageReference Include="PinionCore.Remote.Tools.Protocol.Sources" Version="0.2.0.0">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

定義資料與介面：

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

建立 `ProtocolCreator`（Source Generator 入口）：

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

> 注意：被標記為 `[PinionCore.Remote.Protocol.Creator]` 的方法簽章必須是 `static partial void Method(ref PinionCore.Remote.IProtocol)` 才能編譯。

## 2. Server 專案

建立 Console 專案並加入參考：

```bash
Sample/Server> dotnet new console
```

`csproj` 範例：

```xml
<ItemGroup>
  <PackageReference Include="PinionCore.Remote.Server" Version="0.2.0.0" />
  <ProjectReference Include="..\Protocol\Protocol.csproj" />
</ItemGroup>
```

實作 `IGreeter`：

```csharp
using Protocol;

namespace Server
{
    class Greeter : IGreeter
    {
        PinionCore.Remote.Value<HelloReply> IGreeter.SayHello(HelloRequest request)
        {
            return new HelloReply { Message = $"Hello {request.Name}." };
        }
    }
}
```

實作 `Entry`：

```csharp
using PinionCore.Remote;
using Protocol;

namespace Server
{
    class Entry : IEntry
    {
        public volatile bool Enable = true;
        private readonly Greeter _greeter = new Greeter();

        void ISessionObserver.OnSessionOpened(ISessionBinder binder)
        {
            var soul = binder.Bind<IGreeter>(_greeter);
        }

        void ISessionObserver.OnSessionClosed(ISessionBinder binder)
        {
            Enable = false;
        }

        void IEntry.Update()
        {
        }
    }
}
```

啟動伺服器主程式（TCP 範例）：

```csharp
using System;
using System.Threading.Tasks;
using PinionCore.Remote.Server;
using Protocol;

namespace Server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            int port = int.Parse(args[0]);

            var protocol = ProtocolCreator.Create();
            var entry = new Entry();

            var host = new PinionCore.Remote.Server.Host(entry, protocol);
            PinionCore.Remote.Soul.IService service = host;

            var (disposeServer, errorInfos) = await service.ListenAsync(
                new PinionCore.Remote.Server.Tcp.ListeningEndpoint(port, backlog: 10));

            foreach (var error in errorInfos)
            {
                Console.WriteLine($"Listener error: {error.Exception}");
                return;
            }

            Console.WriteLine("Server started.");

            while (entry.Enable)
            {
                System.Threading.Thread.Sleep(0);
            }

            disposeServer.Dispose();
            host.Dispose();

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
```

## 3. Client 專案

建立 Console 專案並加入參考：

```bash
Sample/Client> dotnet new console
```

`csproj` 範例：

```xml
<ItemGroup>
  <PackageReference Include="PinionCore.Remote.Client" Version="0.2.0.0" />
  <ProjectReference Include="..\Protocol\Protocol.csproj" />
</ItemGroup>
```

使用相同協議連線伺服器：

```csharp
using System.Net;
using PinionCore.Remote.Client;
using Protocol;

namespace Client
{
    internal class Program
    {
        private static bool _enable = true;

        static void Main(string[] args)
        {
            _Run(args).Wait();
        }

        private static async Task _Run(string[] args)
        {
            var ip = IPAddress.Parse(args[0]);
            var port = int.Parse(args[1]);

            var protocol = ProtocolCreator.Create();
            var proxy = new Proxy(protocol);
            var agent = proxy.Agent;

            var endpoint = new PinionCore.Remote.Client.Tcp.ConnectingEndpoint(
                new IPEndPoint(ip, port));

            // Connect() 是 AgentExtensions 的擴充方法
            var connection = await agent.Connect(endpoint).ConfigureAwait(false);

            agent.QueryNotifier<IGreeter>().Supply += greeter =>
            {
                var request = new HelloRequest { Name = "you" };
                greeter.SayHello(request).OnValue += _OnReply;
            };

            while (_enable)
            {
                System.Threading.Thread.Sleep(0);
                agent.HandleMessages();
                agent.HandlePackets();
            }

            connection.Dispose();
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        private static void _OnReply(HelloReply reply)
        {
            Console.WriteLine($"Receive message: {reply.Message}");
            _enable = false;
        }
    }
}
```

- 客戶端使用與伺服器相同的 Protocol 定義。
- 透過 TCP 端點連線，`QueryNotifier<IGreeter>()` 取得遠端介面代理。
- `HandleMessages()` / `HandlePackets()` 必須持續呼叫以處理返回結果與 Notifier 事件。
