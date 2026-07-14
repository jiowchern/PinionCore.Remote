# Changelog

All notable changes to PinionCore Remote will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### 2026-07-14

> Package versions are not bumped yet. When publishing, advance only the modified project(s) per this entry — the affected libraries are **PinionCore.Utility** (`0.2.0.0` → next minor), **PinionCore.Remote** (`0.2.0.0` → next minor), **PinionCore.Remote.Tools.Protocol.Sources**, and **PinionCore.Remote.Reactive**.

#### Added
- `Spirit<T>` — a new remote member type: a single-shot, disposable object supply returned from a Spirit (interface) method. The server (Soul) returns `new Spirit<T>(instance)`; the client (Ghost) receives the remote proxy through the `Supply` event, and the server's `Dispose()` raises `Unsupply` on the client, after which the Spirit never supplies again (an already-disposed Spirit returned from a method supplies nothing). `Supply`/`Unsupply` have replay-on-subscribe semantics. `T` must be an interface. No new opcodes: supply reuses the return-value soul binding (`LoadSoul`/`LoadSoulCompile`), unsupply reuses `UnloadSoul`. (Projects: PinionCore.Utility — `Remote/Spirit.cs`, `ISpiritSoul`, `ISpiritGhost`; PinionCore.Remote — soul/ghost runtime wiring; PinionCore.Remote.Tools.Protocol.Sources — `MethodPinionCoreRemoteSpirit` block modifier)
- Reactive extensions `SupplyEvent()` / `UnsupplyEvent()` over `Spirit<T>` converting the events to `IObservable<T>`. (Project: PinionCore.Remote.Reactive)
- Tests: `SpiritTests` verifying supply, remote method calls on the supplied proxy, unsupply on server dispose, and no supply after dispose. (Project: PinionCore.Remote.Tools.Protocol.Sources.TestCommon.Tests)

#### Fixed
- `SoulMethodHandler._ReturnSoulValue` — the `Value<interface>` method-return path previously threw `NotImplementedException`; it now binds the returned object as a return-type soul so interface-returning `Value<T>` methods work again. (Project: PinionCore.Remote)

### 2026-07-11

> Package versions are not bumped yet. When publishing, advance only the modified project(s) per this entry — the affected library is **PinionCore.Serialization** (`0.2.0.0` → next minor; the wire format is not backward compatible).

#### Changed
- **Breaking (wire format)**: shrank per-field/per-element metadata in the serialized byte stream. Both endpoints must run the same PinionCore.Serialization version — mixed old/new versions cannot exchange data. Small framework messages such as `PackageCallMethod` shrink roughly 40% (28 → 16 bytes in the benchmark). (Project: PinionCore.Serialization)
  - `ClassDescriber`: a class/struct payload now starts with a `ceil(N/8)`-byte field bitmask replacing the previous `[validCount] + per-field [index]` encoding.
  - `ClassDescriber`/`ArrayDescriber`: fields and array elements whose *declared* type is a value type or a sealed class (`string` and array types included) no longer carry a runtime type-id; the decoder derives the describer from the declared type (`TypeIdentifier.IsFinal`, which excludes `Nullable<T>`). Non-sealed class fields still carry a type-id for polymorphism.
  - `ArrayDescriber`: dense arrays (no default-valued elements, e.g. `MethodParams`) omit per-element indices entirely; sparse arrays keep the varint index per written element.
  - `BufferDescriber` (`char[]`): removed the redundant byte-length prefix; only the element count is written and the byte length is derived from the fixed element size.

#### Added
- `TypeIdentifier.IsFinal(Type)` — the shared encode/decode rule for type-id elision. (Project: PinionCore.Serialization)
- Tests: `MetadataCompressionTests` pinning the new sizes (call-method-like package ≤ 16 bytes, dense `int[5]` ≤ 8 bytes, sealed vs non-sealed field size difference) plus round-trip regressions for sparse arrays with default elements, >8-field bitmask spans, all-default instances, and polymorphic fields. (Project: PinionCore.Serialization.Test)

### 2026-07-03

> Package versions are not bumped yet. When publishing, advance only the modified project(s) per this entry — the affected libraries are **PinionCore.Utility** (`0.2.0.0` → next patch) and **PinionCore.Remote** (`0.2.0.0` → next patch).

#### Added
- `INotifier<T>` is now covariant (`INotifier<out T>`). A notifier of a concrete type implicitly converts to a notifier of any interface that type implements — e.g. `INotifier<World>` → `INotifier<IWorld>` — with the inheritance constraint checked at compile time. (Project: PinionCore.Utility)
- `Notifier<T>` gained a `Notifier(INotifier<T> notifier)` constructor for wrapping an existing supply/unsupply source on the server side. The collection parameter of the existing constructor is only exercised by the client-side ghost pipeline (`IObjectAccessible`), so this overload backs it with an inert list. (Project: PinionCore.Remote)
- `NotifierExtensions.ToNotifier<T>()` — exposes a `Depot<TConcrete>` (or any `INotifier` source) as a `Notifier<T>` of a parent interface in one call: `depot.ToNotifier<IWorld>()`. One depot can feed any number of interface notifiers, one line each. Calling it with an interface the item type does not implement is a compile error. (Project: PinionCore.Remote)
- Tests: `DepotTests` (covariant subscription, existing-item replay, mixed exact/variant handlers, handler removal) and `NotifierCastTests` (`ToNotifier` supply/unsupply `TypeObject` identity, multiple notifiers from one depot, dispose). (Projects: PinionCore.Utility.Test, PinionCore.Remote.Test)

#### Changed
- `Depot<T>` and `TProvider<T>` store event handlers in lists instead of multicast delegate fields. Subscribing through a covariant reference passes a delegate whose runtime type differs from the backing field's (`Action<IWorld>` into `Action<World>`), which makes `Delegate.Combine` throw `ArgumentException: Delegates must be of the same type`; list storage accepts both. Any other `INotifier<T>` implementation that keeps field-like events has the same trap if subscribed through a variant reference. (Projects: PinionCore.Utility)

### 2026-07-01

> Package versions are not bumped yet. When publishing, advance only the modified project(s) per this entry — the affected library is **PinionCore.Remote** (`0.2.0.0` → next patch).

#### Added
- `MemberMap` now exposes public read-only accessors for `Interfaces` (`IReadOnlyBilateralMap<int, Type>`), `Methods` (`IReadOnlyDictionary<int, MethodInfo>`), and `Events` (`IReadOnlyDictionary<int, EventInfo>`), mirroring the existing `Properties` accessor. Consumers can now enumerate the full member map — interface/method/event id mappings — instead of going through the individual `GetInterface`/`GetMethod`/`GetEvent` lookups. (Project: PinionCore.Remote)

### 2026-06-29

> Package versions are not bumped yet. When publishing, advance only the modified project(s) per this entry — the affected library is **PinionCore.Remote** (`0.2.0.0` → next patch).

#### Changed
- `Property<T>.IAccessable.Set` now routes through `_SetValue`, so applying a property update (e.g. a ghost receiving a network update) raises `DirtyEvent` like a normal assignment. This lets a relay node observe property changes on a received ghost and forward them onward — enabling live property synchronization across cross-server relay hops. (Project: PinionCore.Remote)

#### Added
- **Cross-Server Interface Relay**: documented and tested the ability to relay an interface across multiple servers. An interface instance created on server B can be handed to server A (acting as B's client) and re-bound to A's own clients, so a client connected only to A can transparently use interfaces sourced from B — with methods (`Value<T>`), properties (`Property<T>`, including live updates), and notifiers (`Notifier<T>` object trees) all working across the hop.
  - Added `PinionCore.Integration.Tests/RelayTests.cs` verifying the `B → A → Client` chain end to end for method, property, and notifier relaying (each guarded with a 5s timeout).
  - Added a "Cross-Server Interface Relay" section with a Mermaid diagram to `docs/readme/en/core-features.md` and `docs/readme/tc/core-features.md`.

### 2025-12-08

#### Changed
- **Breaking (API)**: `IStreamable.Receive` and `IStreamable.Send` now take a `System.Threading.CancellationToken` parameter, so pending stream operations can be aborted on disconnect or shutdown instead of hanging. All transport implementations were updated accordingly. (Project: PinionCore.Network)
- **Breaking (protocol source generation)**: a streamable interface method must now declare a fourth `System.Threading.CancellationToken` parameter — `IAwaitableSource<int> Method(byte[] buffer, int offset, int count, CancellationToken token)` — to be recognized by the Source Generator; the previous three-parameter form is treated as a regular RMI. (Project: PinionCore.Remote.Tools.Protocol.Sources)
- Improved cancellation propagation and error handling across the connection pipeline. (Projects: PinionCore.Network, PinionCore.Remote)

### 2025-11-16

#### Changed
- Upgraded all package versions from `0.1.x.x` to `0.2.0.0` (minor version bump)
  - **Lib Folder Projects**:
    - PinionCore.Remote: `0.1.14.15` → `0.2.0.0`
    - PinionCore.Network: `0.1.14.12` → `0.2.0.0`
    - PinionCore.Serialization: `0.1.14.12` → `0.2.0.0`
    - PinionCore.Remote.Soul: `0.1.14.14` → `0.2.0.0`
    - PinionCore.Remote.Ghost: `0.1.14.13` → `0.2.0.0`
    - PinionCore.Remote.Reactive: `0.1.14.13` → `0.2.0.0`
    - PinionCore.Utility: `0.1.14.14` → `0.2.0.0`
  - **Publish Folder Projects**:
    - PinionCore.Remote.Standalone: `0.1.14.14` → `0.2.0.0`
    - PinionCore.Remote.Server: `0.1.14.13` → `0.2.0.0`
    - PinionCore.Remote.Client: `0.1.14.12` → `0.2.0.0`
    - PinionCore.Remote.Tools.Protocol.Sources: `0.0.4.25` → `0.2.0.0`
    - PinionCore.Remote.Protocol.Identify: `0.0.1.1` → `0.2.0.0`
    - PinionCore.Remote.Gateway: Added version `0.2.0.0` with NuGet packaging support
    - PinionCore.Remote.Gateway.Protocols: Added version `0.2.0.0` with NuGet packaging support

#### Added
- Added support for multiple transport modes (TCP/WebSocket/Standalone) with behavioral consistency verification.
- Added `PinionCore.Remote.Standalone.ListeningEndpoint` class to support Standalone mode listening.
- Added `PinionCore.Remote.Client.Web.ConnectingEndpoint` class for WebSocket connection with error event handling.

#### Changed
- **Breaking**: Renamed core classes for improved clarity:
  - `Soul` → `Host` (server-side service)
  - `Ghost` → `Proxy` (client-side proxy)
  - Internal `User` → `Agent` (in Ghost module)
- Removed forced type conversions in favor of explicit variable declarations to improve code readability and safety.
- Enhanced README documentation with OpenDeepWiki link.
- Reorganized class structure to simplify implementation and improve maintainability.

#### Fixed
- Fixed safe calling of `ErrorEvent` in `Peer.cs` to avoid null reference exceptions.
- Complete implementation of `ListeningEndpoint` class with event handling and resource disposal.
- Removed `NotImplementedException` placeholders with fully functional implementations.

### 2025-11-15

#### Added
- Introduced `Result` structure to encapsulate connection results (including `Peer` and `Exception`).
- Added `ConnectOrThrowAsync` helper method to reduce code duplication and improve readability.
- Added multi-port support and enhanced error handling in connection logic.

#### Changed
- Unified TCP connection exception handling logic to throw `InvalidOperationException` when `Peer` is null.
- Updated all connection logic to use the new `Result` structure for better error handling.

### 2025-11-13

#### Added
- Introduced `ISessionBinder` and `ISessionObserver` interfaces to replace legacy `IBinder` and `IBinderProvider`.
- Added `Ghost` and `Soul` classes as async client proxy and server service wrappers respectively.
- Added serialization optimization proposal with ZigZag encoding and UTF-8 support.

#### Changed
- Converted multiple synchronous methods to async versions (e.g., `ConnectAsync`) for improved async performance.
- Updated namespace structure (e.g., `Soul` → `Remote.Soul`) for better code organization.
- Enhanced session management logic with new interface abstractions.

#### Removed
- Removed legacy `Node` class and related interfaces to simplify code structure.

### 2025-11-11

#### Changed
- Renamed `Agent` to `User` in Gateway module for improved clarity and consistency.

### 2025-11-07

#### Changed
- Updated and refined README documentation with emphasis on framework features.

### 2025-11-06

#### Added
- Added `CompressionBenchmarkTests` to measure ZigZag integer encoding and UTF-8 string serialization performance.

#### Changed
- Local in-process hosting now uses `PinionCore.Remote.Soul.Service` with Ghost agents, replacing the legacy Standalone service helpers.
- TCP and WebSocket connectors now return `Peer` instances directly and expect `Peer.Disconnect()` for unified teardown.
- Gateway agent ping reporting now reads from the shared pool metrics instead of recalculating averages on each call.

#### Fixed
- WebSocket connection handling now validates the returned `Peer` and surfaces socket errors for more reliable disconnect detection in tests.

### 2025-10-21

#### Added
- **Gateway Protocol Versioning Support**: Router now supports multiple protocol versions simultaneously
  - `SessionHub` implements version-based isolation using `VersionKey` for safe hash-based dictionary operations
  - `ILineAllocatable` interface now includes `Version` property for protocol version identification
  - `ILoginable.Login()` method now accepts `byte[] version` parameter for version-aware authentication
  - Automatic isolation of clients and services based on `IProtocol.VersionCode`
  - Support for blue-green deployment and canary release strategies
- New `ISessionMembershipProvider` interface for version-aware session management
- New `Entry` class replacing `ClientEntry` for improved client connection handling
- Enhanced protocol version documentation in Gateway README with upgrade strategies

#### Changed
- **Breaking**: `ILoginable.Login(uint group)` signature changed to `Login(uint group, byte[] version)`
- **Breaking**: `IServiceRegistry.Register/Unregister` methods now take `ILineAllocatable` directly instead of separate `uint group` parameter
- `SessionHub` refactored from simple wrapper to version management center
  - Now maintains multiple `SessionCoordinator` instances, one per protocol version
  - Implements both `ISessionMembershipProvider` and `IServiceRegistry` interfaces
- `Registry` constructor now requires protocol version: `Registry(IProtocol protocol, uint group)`
- Improved hash collision handling with dedicated `VersionKey` class using Base64 encoding

#### Removed
- `ClientEntry.cs` - functionality merged into new `Entry` class

#### Fixed
- Thread safety improvements in `SessionHub` version coordinator management
- Memory leak prevention with proper `Dispose` implementation for version coordinators

## [0.1.11.12] - 2025-10-21

### Added
- Gateway module routing architecture improvements
- Comprehensive test coverage for multi-service scenarios

### Changed
- Renamed `Host` to `Router` for clearer architectural intent
- Improved routing strategy implementation with unified naming conventions
- Updated RoundRobin selector class naming

### Fixed
- CI/CD pipeline improvements with explicit solution file specification

## [0.1.11.11] - 2025-10-21

### Added
- Gateway test annotations and README documentation
- Reactive Extensions (Rx) integration for event handling
- Session coordination functionality

### Changed
- Refactored test logic with multiple game services and registries
- Enhanced connection management with new session coordinator

## [0.1.11.10-alpha] - 2025-10-20

### Added
- New Gateway project and related functionality
- Chat system features and related projects

### Changed
- Refactored proxy, server, and notification systems
- Updated PinionCore.Utility subproject version

## [0.1.11.9] - 2025-10-19

### Added
- Streamable method support and related features
- Exception handling enhancements

### Changed
- Refactored Gateway module for improved maintainability and modularity
- Optimized reverse view processing and event triggering logic
- Simplified Gateway architecture with redundancy removal

## [0.1.11.0] - 2025-09-27

### Added
- Connection management and group reset functionality
- Event aggregator class for improved event handling

### Changed
- Refactored connection interface and management logic
- Unified naming structure for event processing
- Replaced all lobby-related classes and interfaces

### Removed
- Serializer class and related methods

### Fixed
- Improved exception handling in ClientReleasedEvent

---

## Migration Guides

### Migrating to Unreleased Version (Protocol Versioning Support)

#### For Registry Users

**Before:**
```csharp
var registry = new Registry(groupId: 1);
```

**After:**
```csharp
var protocol = ProtocolCreator.Create();
var registry = new Registry(protocol, groupId: 1);
```

#### For Custom ILineAllocatable Implementations

**Before:**
```csharp
public class MyAllocator : ILineAllocatable
{
    public uint Group => _group;
    // ...
}
```

**After:**
```csharp
public class MyAllocator : ILineAllocatable
{
    public uint Group => _group;
    public byte[] Version => _protocol.VersionCode;
    // ...
}
```

#### For Service Registry Implementations

**Before:**
```csharp
serviceRegistry.Register(group, allocatable);
serviceRegistry.Unregister(group, allocatable);
```

**After:**
```csharp
serviceRegistry.Register(allocatable);
serviceRegistry.Unregister(allocatable);
```

#### Protocol Version Isolation Example

```csharp
// Deploy multiple protocol versions simultaneously
var protocolV1 = ProtocolV1Creator.Create(); // VersionCode = [1, 0, 0]
var protocolV2 = ProtocolV2Creator.Create(); // VersionCode = [2, 0, 0]

var registryV1 = new Registry(protocolV1, groupId: 1);
var registryV2 = new Registry(protocolV2, groupId: 1);

// Both can register to the same Router
registryV1.Agent.Connect(router.Registry);
registryV2.Agent.Connect(router.Registry);

// Clients with V1 protocol only connect to registryV1
// Clients with V2 protocol only connect to registryV2
```

---

## Links

- [Documentation](README.md)
- [Gateway Documentation](PinionCore.Remote.Gateway/README.md)
- [Issue Tracker](https://github.com/jiowchern/PinionCore.Remote/issues)
- [Repository](https://github.com/jiowchern/PinionCore.Remote)

[Unreleased]: https://github.com/jiowchern/PinionCore.Remote/compare/0.1.11.12...HEAD
[0.1.11.12]: https://github.com/jiowchern/PinionCore.Remote/compare/0.1.11.11...0.1.11.12
[0.1.11.11]: https://github.com/jiowchern/PinionCore.Remote/compare/0.1.11.10-alpha...0.1.11.11
[0.1.11.10-alpha]: https://github.com/jiowchern/PinionCore.Remote/compare/0.1.11.9...0.1.11.10-alpha
[0.1.11.9]: https://github.com/jiowchern/PinionCore.Remote/compare/0.1.11.0...0.1.11.9
[0.1.11.0]: https://github.com/jiowchern/PinionCore.Remote/releases/tag/0.1.11.0
