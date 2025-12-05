# Advanced Topics

[Back: Transport Modes](transports.md) | [Next: Samples & Conclusion](samples-and-tests.md)

## Reactive Extensions (PinionCore.Remote.Reactive)

`PinionCore.Remote.Reactive/Extensions.cs` provides the following commonly used extensions:

- `ReturnVoid(this Action)` — converts an `Action` into `IObservable<Unit>`
- `RemoteValue(this Value<T>)` — converts a remote return value into `IObservable<T>`
- `PropertyChangeValue(this Property<T>)` — converts property change notifications into `IObservable<T>`
- `SupplyEvent` / `UnsupplyEvent` — converts `INotifier<T>` supply/unsupply events into `IObservable<T>`

These extensions allow you to build remote workflows seamlessly using Rx + LINQ, offering a more expressive, functional style on top of the event-driven model.

---

## Gateway Module

`PinionCore.Remote.Gateway` provides:

- Multiple service entry points (Router)
- Grouping & load balancing (LineAllocator)
- Version coexistence (different `IProtocol.VersionCode`)
- Real-world usage integrated with `PinionCore.Consoles.Chat1.*` as examples

Key use cases:

- Centralized routing for multiple backend services
- Upgrading protocol versions without breaking existing clients
- Distributing connections across multiple service lines

For details, refer to `PinionCore.Remote.Gateway/README.md` and the chat-related sample projects.

---

## Custom Connection

If the built-in TCP / WebSocket transports don’t satisfy your needs, you can implement your own transport layer.

You may create custom implementations of:

- `PinionCore.Network.IStreamable` (controls low-level byte[] send/receive behavior)
- `PinionCore.Remote.Client.IConnectingEndpoint`
- `PinionCore.Remote.Server.IListeningEndpoint`

These custom endpoints behave the same as the built-in ones, just with your own underlying protocol or data channel.

---

## Custom Serialization

To use custom serialization, it is recommended to directly set up the low-level classes (instead of using the simplified wrappers).

Server-side (using `Soul.Service`):

```csharp
var serializer = new YourSerializer();
var internalSerializer = new YourInternalSerializer();
var pool = PinionCore.Memorys.PoolProvider.Shared;

var service = new PinionCore.Remote.Soul.Service(
    entry, protocol, serializer, internalSerializer, pool);
```

Client-side (using `Ghost.Agent`):

```csharp
var serializer = new YourSerializer();
var internalSerializer = new YourInternalSerializer();
var pool = PinionCore.Memorys.PoolProvider.Shared;

var agent = new PinionCore.Remote.Ghost.Agent(
    protocol, serializer, internalSerializer, pool);
```

Notes:

- Required serialization types can be obtained from `IProtocol.SerializeTypes`.
- Additional details can be found in `PinionCore.Serialization/README.md`.
