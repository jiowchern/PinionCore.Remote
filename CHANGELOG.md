# Changelog

All notable changes to PinionCore Remote will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
