# Architecture & Module Overview

[Back: Core Features](core-features.md) | [Next: Quick Start](quick-start.md)

Main projects and their roles:

- **PinionCore.Remote**
  - Core interfaces and abstractions: `IEntry`, `ISessionBinder`, `ISoul`
  - State types: `Value<T>`, `Property<T>`, `Notifier<T>`

- **PinionCore.Remote.Client**
  - `Proxy`, `IConnectingEndpoint`
  - Connection utilities: `AgentExtensions.Connect`

- **PinionCore.Remote.Server**
  - `Host`, `IListeningEndpoint`
  - Service startup & listening: `ServiceExtensions.ListenAsync`

- **PinionCore.Remote.Soul**
  - Server-side session management (`SessionEngine`)
  - Update loop: `ServiceUpdateLoop`

- **PinionCore.Remote.Ghost**
  - Client `Agent` implementation (`User`)
  - Packet encoding & processing

- **PinionCore.Remote.Standalone**
  - `ListeningEndpoint` that simulates both server and client in-memory

- **PinionCore.Network**
  - `IStreamable` interface, TCP/WebSocket peers, packet read/write utilities

- **PinionCore.Serialization**
  - Default serialization implementation and type descriptors (customizable)

- **PinionCore.Remote.Tools.Protocol.Sources**
  - Source Generator
  - Annotated with `[PinionCore.Remote.Protocol.Creator]` to auto-generate `IProtocol`

- **PinionCore.Remote.Gateway**
  - Gateway / Router, multi-service routing, version coexistence
  - See the module README for more details
