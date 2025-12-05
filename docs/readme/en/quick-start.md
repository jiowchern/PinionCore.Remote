# Quick Start (Hello World)

[Back: Architecture & Modules](architecture.md) | [Next: Core Concepts](core-concepts.md)

It is recommended to create three separate projects: **Protocol**, **Server**, and **Client**. The following example is a simplified version. Full samples can be found in:

- `PinionCore.Samples.HelloWorld.Protocols`
- `PinionCore.Samples.HelloWorld.Server`
- `PinionCore.Samples.HelloWorld.Client`

## Environment Requirements

- .NET SDK 6 or later
- Visual Studio 2022 / JetBrains Rider / VS Code
- For Unity: Unity 2021 LTS or newer is recommended

## 1. Protocol Project

Create a Class Library:

```bash
Sample/Protocol> dotnet new classlib
```

Add NuGet references (version numbers may vary):

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

Define data structures & interfaces (HelloWorld example):

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

Create `ProtocolCreator` (Source Generator entry point):

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

## 2. Server Project

Create console app and add references to Protocol + `PinionCore.Remote.Server` & `PinionCore.Remote.Soul`:

```bash
Sample/Server> dotnet new console
```

Implement server entry and bind your interface:

```csharp
using PinionCore.Remote;
using PinionCore.Remote.Server;
using Protocol;

namespace Server
{
    public class Entry : IEntry
    {
        void ISessionObserver.OnSessionOpened(ISessionBinder binder)
        {
            binder.Bind<IGreeter>(new Greeter());
        }

        void ISessionObserver.OnSessionClosed(ISessionBinder binder)
        {
        }

        void IEntry.Update()
        {
        }
    }

    public class Greeter : IGreeter
    {
        public Value<HelloReply> SayHello(HelloRequest request)
        {
            return new HelloReply { Message = $"Hello {request.Name}." };
        }
    }
}
```

Start the server:

```csharp
using var host = new Host(new Entry(), ProtocolCreator.Create());
IService service = host;
var (disposeServer, errors) = await service.ListenAsync(
    new PinionCore.Remote.Server.Tcp.ListeningEndpoint(5000, backlog: 10));
```

## 3. Client Project

Create console app and add references to Protocol + `PinionCore.Remote.Client`:

```bash
Sample/Client> dotnet new console
```

Use the same protocol and connect to the server:

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

            // Connect() is in AgentExtensions
            var connection = await agent.Connect(endpoint).ConfigureAwait(false);

            agent.QueryNotifier<IGreeter>().Supply += greeter =>
            {
                var request = new HelloRequest { Name = "you" };
                greeter.SayHello(request).OnValue += _OnReply;
            };

            // Must continuously process packets and messages
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

Explanation:

- The client creates a `Proxy` based on the same protocol definition as the server.
- It connects to the server using a TCP endpoint.
- After connection, `QueryNotifier<IGreeter>()` returns the remote interface proxy.
- The remote `SayHello()` behaves just like a local async call.
- `HandleMessages()` and `HandlePackets()` **must** be called repeatedly so the client can process incoming remote values and notifier events.
