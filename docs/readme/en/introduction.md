# PinionCore Remote – Introduction

[Back to main README](../../README.md) | [Next: Core Features](core-features.md)

**PinionCore Remote** is an interface-oriented remote communication framework built in C#.

You define **interfaces** as remote protocols. Servers implement these interfaces, and clients invoke them as if they were local methods; actual data is transmitted through **TCP / WebSocket / Standalone (in-process simulation)**.

- Supports **.NET Standard 2.1** (.NET 6/7/8, Unity 2021+)
- Supports **IL2CPP & AOT** (requires pre-registered serialization types)
- Built-in **TCP**, **WebSocket**, and **Standalone** transport modes
- Uses **Source Generator** to automatically generate `IProtocol` implementation
- Based on **Value / Property / Notifier** to describe remote behaviors & states
- Works with **PinionCore.Remote.Reactive** to write remote workflows in Rx style

## Online Documentation

- [DeepWiki](https://deepwiki.com/jiowchern/PinionCore.Remote)
- [OpenDeepWiki](https://opendeep.wiki/jiowchern/PinionCore.Remote/introduction?branch=master)
