# Detailed Core Concepts

[Back: Quick Start](quick-start.md) | [Next: Transport Modes](transports.md)

## Spirit / Soul / Ghost

The framework's terminology forms a trinity:

- **Spirit** — the communication interface: a plain C# interface shared by server and client, defining methods (`Value<T>`), properties (`Property<T>`), events, notifiers, and `Spirit<T>`.
- **Soul** — the server-side incarnation of a Spirit: the implementation bound to a session via `Bind<T>` (see `PinionCore.Remote.Soul`).
- **Ghost** — the client-side incarnation of a Spirit: the live proxy obtained via `QueryNotifier<T>` (see `PinionCore.Remote.Ghost`).

The server binds a Soul; the client receives its Ghost. Both program against the same Spirit.

---

## IEntry / ISessionBinder / ISoul

- **`IEntry`**
  The server entry point. Handles session open/close events and optional per-frame updates.

- **`ISessionBinder`**
  Passed into `OnSessionOpened`. Used to `Bind<T>` and `Unbind(ISoul)` remote interface implementations.

- **`ISoul`**
  Represents a bound instance within a session. Returned from `Bind<T>`, and can later be passed to `Unbind`.

Related files:

- `PinionCore.Remote/IEntry.cs`
- `PinionCore.Remote/ISessionObserver.cs`
- `PinionCore.Remote/ISessionBinder.cs`
- `PinionCore.Remote/ISoul.cs`

`PinionCore.Remote.Soul.Service` internally manages sessions using `SessionEngine`. `PinionCore.Remote.Server.Host` wraps the service to create a runnable server.

---

## Value<T>

Characteristics:

- Supports `OnValue` events and `await`.
- The value is assigned **only once** (one-time result).
- Supports implicit conversion: writing `return new HelloReply { ... };` automatically becomes `Value<HelloReply>`.

Implementation file: `PinionCore.Utility/Remote/Value.cs`

---

## Property<T>

A notifiable state value:

- Setting `.Value` triggers a DirtyEvent.
- Can be converted into `IObservable<T>` via `PropertyObservable` (see `PinionCore.Remote.Reactive/PropertyObservable.cs`).
- Implicit conversion to `T` is supported, making usage similar to a normal property.

Implementation file: `PinionCore.Remote/Property.cs`

---

## Notifier<T> and Depot<T>

`Depot<T>` (located in `PinionCore.Utility/Remote/Depot.cs`) is a combination of:

- A collection of items
- Notification events when items are added or removed

Behavior:

- `Items.Add(item)` → triggers **Supply**
- `Items.Remove(item)` → triggers **Unsupply**

`INotifier<out T>` is covariant: a `Depot<World>` implicitly converts to an
`INotifier<IWorld>` (`World : IWorld`, checked at compile time). Combined with
the `ToNotifier<T>()` extension, one depot can feed a `Notifier<T>` per
implemented interface (create each once and keep it in a field — do not call
`ToNotifier` inside a property getter):

```csharp
var depot = new PinionCore.Remote.Depot<World>();
var worldNotifier = depot.ToNotifier<IWorld>();   // Notifier<IWorld>
var viewNotifier = depot.ToNotifier<IView>();     // Notifier<IView>
```

`Notifier<T>` wraps `Depot<TypeObject>` and supports:

- Cross-type querying
- Subscription to supply/unsupply events

The `INotifierQueryable` interface (in `PinionCore.Remote/INotifierQueryable.cs`) provides:

```csharp
INotifier<T> QueryNotifier<T>();
```

The client-side `Ghost.User` implements `INotifierQueryable`, meaning:

- Any remote interface collection can be discovered dynamically
- The client does **not** need to manage IDs or registries (the Notifier system handles remote object lifecycle synchronization automatically)

---

## Spirit<T>

> Naming note: the term **Spirit** refers to the communication interface itself; the `Spirit<T>` class is a container that carries a single interface instance — a method returning `Spirit<T>` delivers exactly one incarnation of a Spirit (interface).

A single-shot remote object container returned from a method:

- The server wraps an implementation with `new Spirit<T>(instance)` and returns it (`T` must be an interface).
- The client-side `Spirit<T>` delivers the remote proxy (Ghost) through its `Supply` event.
- After the server calls `Dispose()`, the client receives `Unsupply` and the Spirit never supplies again.
- `Supply` / `Unsupply` have replay semantics: late subscribers still receive events that already happened.
- `SupplyEvent()` / `UnsupplyEvent()` in `PinionCore.Remote.Reactive` convert them to `IObservable<T>`.

```csharp
public interface ILobby
{
    PinionCore.Remote.Spirit<IRoom> EnterRoom(int roomId);
}
```

Implementation file: `PinionCore.Utility/Remote/Spirit.cs`
Test example: `PinionCore.Remote.Tools.Protocol.Sources.TestCommon.Tests/SpiritTests.cs`

---

## Streamable Methods

If an interface method is defined like this:

```csharp
PinionCore.Remote.IAwaitableSource<int> StreamEcho(
    byte[] buffer,
    int offset,
    int count,
    System.Threading.CancellationToken token);
```

The Source Generator automatically treats it as a **streamable method**. The signature must match exactly —
four parameters ending with a `CancellationToken` — otherwise the method is treated as a regular RMI.

Behavior:

- Sending data: only the slice `buffer[offset .. offset + count)` is transmitted.
- Server processing: the server writes the processed data **back into the same buffer region**.
- Return value `IAwaitableSource<int>` indicates **the actual number of bytes processed**.
- Cancelling the `token` aborts the pending stream operation.

The detection logic for streamable methods is located in:

```
PinionCore.Remote.Tools.Protocol.Sources/MethodPinionCoreRemoteStreamable.cs
```
