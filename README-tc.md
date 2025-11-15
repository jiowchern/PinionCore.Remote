
  # PinionCore Remote
  [![Maintainability](https://api.codeclimate.com/v1/badges/89c3a646f9daff42a38e/maintainability)](https://codeclimate.com/github/jiowchern/PinionCore.Remote/maintainability)
  [![Build](https://github.com/jiowchern/PinionCore.Remote/actions/workflows/dotnet-desktop.yml/badge.svg?branch=master)](https://github.com/jiowchern/PinionCore.Remote/actions/workflows/dotnet-desktop.yml)
  [![Coverage Status](https://coveralls.io/repos/github/jiowchern/PinionCore.Remote/badge.svg?branch=master)](https://coveralls.io/github/jiowchern/PinionCore.Remote?branch=master)
  ![commit last date](https://img.shields.io/github/last-commit/jiowchern/PinionCore.Remote)

  ## 簡介
  PinionCore Remote 是以 C# 開發的物件導向遠端通訊框架。伺服器由 `Soul` 管理連線生命週期，客戶端透過 `Ghost` 直接呼叫介面。Source Generator 自動產生協議，讓程式碼只需專注在介面實作，即可在 TCP / WebSocket / Standalone 等傳輸模式下運作。

  ## 支援
  - IL2CPP 與 AOT 平台。
  - .NET Standard 2.1（對應 .NET 6/7/8、Unity 2021 LTS+）。
  - 內建 TCP (`PinionCore.Remote.Server.Tcp.ListeningEndpoint`、`PinionCore.Remote.Client.Tcp.ConnectingEndpoint`)、WebSocket (`PinionCore.Remote.Server.Web.ListeningEndpoint`、`PinionCore.Remote.Client.Web.ConnectingEndpoint`) 與單機模
  擬 (`PinionCore.Remote.Standalone.ListeningEndpoint`)。
  - 預設序列化器 `PinionCore.Remote.Serializer`，並支援自訂序列化。
  - 協議版本比對、Notifier/Property 供應與事件同步。

  ## 模組快覽
  - **PinionCore.Remote**：核心介面、Value/Property/Notifier 等抽象。
  - **PinionCore.Remote.Client**：`Ghost`、連線端點與 Agent 擴充。
  - **PinionCore.Remote.Server**：`Soul`、`ServiceExtensions.ListenAsync` 與監聽端點。
  - **PinionCore.Remote.Soul**：伺服器 Session 與更新迴圈。
  - **PinionCore.Remote.Ghost**：客戶端 Agent 與封包處理。
  - **PinionCore.Remote.Standalone**：無網路環境的 Stream 模擬。
  - **PinionCore.Network**：`IStreamable`、`PackageReader/Sender` 等流層抽象。
  - **PinionCore.Serialization**：預設序列化工具。
  - **PinionCore.Remote.Tools.Protocol.Sources**：協議 Source Generator。
  - **PinionCore.Remote.Gateway**：多服務閘道/路由器。

  ## 快速預覽
  ### Protocol 介面
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
  ### 伺服器（Entry + Soul + ListenAsync）
  ```csharp
  public class Entry : PinionCore.Remote.IEntry
  {
      private readonly Protocol.IGreeter _greeter = new Greeter();

      void ISessionObserver.OnSessionOpened(PinionCore.Remote.ISessionBinder binder)
      {
          binder.Bind<Protocol.IGreeter>(_greeter);
      }

      void ISessionObserver.OnSessionClosed(PinionCore.Remote.ISessionBinder binder) { }
      void PinionCore.Remote.IEntry.Update() { }
  }

  var protocol = ProtocolCreator.Create();
  var entry = new Entry();
  var soul = new PinionCore.Remote.Server.Soul(entry, protocol);
  var standalone = new PinionCore.Remote.Standalone.ListeningEndpoint();

  var (disposeServer, errors) = await soul.ListenAsync(
      new PinionCore.Remote.Server.Tcp.ListeningEndpoint(tcpPort: 7000, backlog: 64),
      new PinionCore.Remote.Server.Web.ListeningEndpoint($"http://localhost:{webPort}/"),
      standalone);

  if (errors.Length > 0)
  {
      foreach (var err in errors)
      {
          Console.WriteLine($"Listener error: {err.Exception}");
      }
      return;
  }

  // ... 服務運行
  disposeServer.Dispose();
  soul.Dispose();
  ```
  ### 客戶端（Ghost + Agent）
  ```csharp
  var protocol = ProtocolCreator.Create();
  var ghost = new PinionCore.Remote.Client.Ghost(protocol);
  using var connection = await ghost.Connect(
      new PinionCore.Remote.Client.Tcp.ConnectingEndpoint(
          new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 7000)));

  ghost.User.QueryNotifier<Protocol.IGreeter>().Supply += async greeter =>
  {
      var reply = await greeter.SayHello(new HelloRequest { Name = "you" });
      Console.WriteLine(reply.Message);
  };

  using var loopCts = new System.Threading.CancellationTokenSource();
  var loop = Task.Run(async () =>
  {
      while (!loopCts.IsCancellationRequested)
      {
          ghost.User.HandlePackets();
          ghost.User.HandleMessages();
          await Task.Delay(1, loopCts.Token);
      }
  });

  Console.ReadKey();
  loopCts.Cancel();
  await loop;
  connection.Dispose();
  ghost.User.Disable();
  ```
  ### 整合測試（SampleTests）
  
  PinionCore.Integration.Tests/SampleTests.cs:16 同時啟動 TCP、WebSocket 與 Standalone 端點（第 30 行），並使用三個 Ghost 驗證 Echo。RunGhostEchoTestAsync（第 79 行）展示：

  - PinionCore.Remote.Client.AgentExtensions.Connect 連線並建置 Ghost。
  - 背景 Task 以 HandlePackets/HandleMessages 處理封包（第 113 行）。
  - 搭配 PinionCore.Remote.Reactive.Extensions.SupplyEvent() 與 Value<T>.RemoteValue() 形成 LINQ Query，最後以 FirstAsync() 取得結果（第 90 行）。

  ## 特色

  ### 1. 介面導向通訊

  [PinionCore.Remote.Protocol.Creator] 產生 IProtocol 後，伺服器只需在 Entry 內 binder.Bind<T>（PinionCore.Samples.HelloWorld.Server/Entry.cs:17），客戶端再透過 QueryNotifier<T> 取得代理（PinionCore.Samples.HelloWorld.Client/Program.cs:27）。
  PinionCore.Remote.Value<T>（PinionCore.Utility/PinionCore.Utility/Remote/Value.cs:8）實作 IAwaitableSource<T>，可 await 或訂閱 OnValue。

  ### 2. 輕量級模組化

  各模組均採 netstandard2.1（例如 PinionCore.Remote.Client.csproj、PinionCore.Remote.Server.csproj），依需求選配即可。同時僅會載入 IProtocol.SerializeTypes 列出的型別，減少輸送成本。

  ### 3. 可控的生命週期

  ISessionBinder 在 IEntry.OnSessionOpened 傳入，使用 Bind<T>/Unbind(ISoul) 控制供給（PinionCore.Samples.HelloWorld.Server/Entry.cs:17-38）。這讓狀態管理、資源釋放與 StatusMachine 完整落在伺服器端。

  ### 4. 即時通知機制

  Notifier<T>（PinionCore.Remote/Notifier.cs:6）搭配 Depot<T>（PinionCore.Utility/PinionCore.Utility/Remote/Depot.cs:7）可維護人物清單等資料。伺服器只需對 Depot.Items 呼叫 Add/Remove，客戶端就能透過 INotifier<T> 的 Supply/Unsupply 或 Rx 觀察
  序列接收即時變更。

  ### 5. 巢狀介面支援

  介面可繼承並使用 Property<T>（PinionCore.Remote/Property.cs:6）、Value<T>、event 及 Notifier，例如：

  public interface IActor
  {
      PinionCore.Remote.Property<string> Name { get; }
      PinionCore.Remote.Property<int> Level { get; }
  }

  public interface IPlayer : IActor
  {
      PinionCore.Remote.Notifier<IActor> VisibleActors { get; }
      PinionCore.Remote.Property<int> Gold { get; }
      PinionCore.Remote.Value<Path> Move(Position target);
      event System.Action<Position> StopEvent;
  }

  前端操作這些介面就像本地物件，事件與屬性變更由框架同步。
  
  ### 6. 響應式方法支援
  
  PinionCore.Remote.Reactive.Extensions（PinionCore.Remote.Reactive/Extensions.cs:33）提供 SupplyEvent()、RemoteValue() 等擴充，透過 Rx 輕鬆組合 Value、Property、Event。SampleTests 以 LINQ 查詢管線串接連線供應與遠端回傳，示範非同步流程的簡潔
  寫法。

  ### 7. 閘道服務器支援

  PinionCore.Remote.Gateway 透過 Router/Registry/AgentPool 將客戶端導向多個後端，支援群組路由、版本共存與單機模式。詳細說明見 PinionCore.Remote.Gateway/README.md:1。

  ### 8. 單機模擬支援

  PinionCore.Remote.Standalone.ListeningEndpoint（PinionCore.Remote.Standalone/ListeningEndpoint.cs:12）同時實作 IListeningEndpoint 與 IConnectingEndpoint，在無網路情況仍能讓 Soul 與 Ghost 互通。SampleTests 也使用此機制驗證序列化與事件流程。

  ## 使用方式

  1. 定義介面：在 Protocol 專案撰寫介面與資料結構（見前述範例），方法回傳 PinionCore.Remote.Value<T>。
  2. 伺服器實作介面：例如 Greeter 直接回傳 HelloReply，由 Value<T> 隱含轉型。
  3. 撰寫 IEntry：於 OnSessionOpened 使用 binder.Bind<T>，OnSessionClosed 釋放 ISoul 或推進 StatusMachine。
  4. 客戶端透過 Notifier 取得代理：ghost.User.QueryNotifier<T>() 會在伺服器供應介面時觸發 Supply，可 await 方法或使用 RemoteValue()/OnValue 取得結果。

  ## 規格

  - 介面型態：支援 Method、Event、Property、Notifier，詳細定義請參閱 document/communications-method.md:1、document/communications-event.md:1、document/communications-property.md:1、document/communications-notifier.md:1。
  - 串流方法：若介面方法簽名為 PinionCore.Remote.IAwaitableSource<int> Method(byte[] buffer, int offset, int count)，Source Generator 會以 MethodPinionCoreRemoteStreamable.cs:110 的邏輯處理，僅傳輸指定緩衝區區段並在原位覆寫結果。
  - 序列化型別：預設支援型別列在 PinionCore.Serialization/README.md:1，也可透過 IProtocol.SerializeTypes 查詢。

  ———

  ## 快速開始（Getting Started）

  ### 需求

  - Visual Studio 2022 17.8+、Rider 或 VS Code + .NET 6 SDK。
  - Unity 2021 LTS+（IL2CPP 需先註冊序列化型別）。
  - Windows / macOS / Linux 皆可使用 dotnet CLI。

  ### Protocol 專案
  ```
  Sample/Protocol> dotnet new classlib
  ```
  csproj 參考：
  ```xml
  <ItemGroup>
    <PackageReference Include="PinionCore.Remote" Version="0.1.14.15" />
    <PackageReference Include="PinionCore.Serialization" Version="0.1.14.12" />
    <PackageReference Include="PinionCore.Remote.Tools.Protocol.Sources" Version="0.0.4.25">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
  ```
  新增介面與 ProtocolCreator（如「Protocol 介面」範例）。

  ### Server 專案

  Sample/Server> dotnet new console

  csproj 參考：
  ```xml
  <ItemGroup>
    <PackageReference Include="PinionCore.Remote.Server" Version="0.1.14.13" />
    <ProjectReference Include="..\Protocol\Protocol.csproj" />
  </ItemGroup>
  ```
  啟動程式：
  ```csharp
  var protocol = Protocol.ProtocolCreator.Create();
  var entry = new Entry();
  var soul = new PinionCore.Remote.Server.Soul(entry, protocol);
  var (stopServer, errors) = await soul.ListenAsync(
      new PinionCore.Remote.Server.Tcp.ListeningEndpoint(port, 64));

  if (errors.Length == 0)
  {
      Console.WriteLine("Server started.");
      Console.ReadKey();
  }

  stopServer.Dispose();
  soul.Dispose();
  ```
  ### Client 專案
  ```
  Sample/Client> dotnet new console
  ```
  csproj 參考：
  ```xml
  <ItemGroup>
    <PackageReference Include="PinionCore.Remote.Client" Version="0.1.14.12" />
    <PackageReference Include="PinionCore.Remote.Reactive" Version="0.1.14.13" />
    <ProjectReference Include="..\Protocol\Protocol.csproj" />
  </ItemGroup>
  ```
  程式範例：
  ```csharp
  var protocol = Protocol.ProtocolCreator.Create();
  var ghost = new PinionCore.Remote.Client.Ghost(protocol);
  using var connection = await ghost.Connect(
      new PinionCore.Remote.Client.Tcp.ConnectingEndpoint(
          new IPEndPoint(IPAddress.Loopback, port)));

  ghost.User.QueryNotifier<Protocol.IGreeter>().Supply += async greeter =>
  {
      var reply = await greeter.SayHello(new HelloRequest { Name = "demo" });
      Console.WriteLine(reply.Message);
  };

  while (true)
  {
      ghost.User.HandlePackets();
      ghost.User.HandleMessages();
      await Task.Delay(1);
  }
  ```
  ## Standalone（單機）
  ```csharp
  var protocol = ProtocolCreator.Create();
  var entry = new Entry();
  var soul = new PinionCore.Remote.Server.Soul(entry, protocol);
  var standalone = new PinionCore.Remote.Standalone.ListeningEndpoint();
  var (stopServer, _) = await soul.ListenAsync(standalone);

  var ghost = new PinionCore.Remote.Client.Ghost(protocol);
  using var connection = await ghost.Connect(standalone);

  ghost.User.QueryNotifier<Protocol.IGreeter>().Supply += async greeter =>
  {
      Console.WriteLine((await greeter.SayHello(new HelloRequest { Name = "offline" })).Message);
  };
  ```
  ListeningEndpoint 建立一對 PinionCore.Network.Stream，Dispose() 時會通知對端離線。

  ## 自訂連線（Custom Connection）

  實作 PinionCore.Remote.Client.IConnectingEndpoint（PinionCore.Remote.Client/IConnectingEndpoint.cs:5）與 PinionCore.Remote.Server.IListeningEndpoint（PinionCore.Remote.Server/IListeningEndpoint.cs:5）即可接上自訂傳輸層。IStreamable 定義於
  PinionCore.Network/IStreamable.cs:5，提供 Receive/Send 非同步操作。

  ## 自訂序列化（Custom Serialization）

  使用 PinionCore.Remote.Soul.Service 與 PinionCore.Remote.Ghost.User 的完整建構子（PinionCore.Remote.Soul/Service.cs:16、PinionCore.Remote.Ghost/User.cs:26）即可替換序列化器：
  ```csharp
  var serializer = new CustomSerializer();
  var internalSerializer = new CustomInternalSerializer();
  var pool = PinionCore.Memorys.PoolProvider.Shared;

  var soul = new PinionCore.Remote.Soul.Service(entry, protocol, serializer, internalSerializer, pool);
  var ghost = new PinionCore.Remote.Ghost.User(protocol, serializer, internalSerializer, pool);
  ```
  需序列化的型別可由 IProtocol.SerializeTypes（PinionCore.Remote/IProtocol.cs:3）取得，或參考 PinionCore.Serialization/README.md:1。

  ## Gateway 模組

  PinionCore.Remote.Gateway 提供多服務入口、群組與版本路由、透明代理與單機調試能力，詳細 API 與部署方式見 PinionCore.Remote.Gateway/README.md:1。

  ## 建置與測試
  ```
  dotnet restore
  dotnet build --configuration Release --no-restore
  dotnet test /p:CollectCoverage=true /p:CoverletOutput=../CoverageResults/ /p:MergeWith="../CoverageResults/coverage.json" /p:CoverletOutputFormat="lcov%2cjson" -m:1
  dotnet pack --configuration Release --output ./nupkgs
  ```  
