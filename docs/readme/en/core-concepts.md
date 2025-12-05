# Detailed Core Concepts

[Back: Quick Start](quick-start.md) | [Next: Transport Modes](transports.md)

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

## Streamable Methods

If an interface method is defined like this:

```csharp
PinionCore.Remote.IAwaitableSource<int> StreamEcho(
    byte[] buffer,
    int offset,
    int count);
```

The Source Generator automatically treats it as a **streamable method**.

Behavior:

- Sending data: only the slice `buffer[offset .. offset + count)` is transmitted.
- Server processing: the server writes the processed data **back into the same buffer region**.
- Return value `IAwaitableSource<int>` indicates **the actual number of bytes processed**.

The detection logic for streamable methods is located in:

```
PinionCore.Remote.Tools.Protocol.Sources/MethodPinionCoreRemoteStreamable.cs
```
