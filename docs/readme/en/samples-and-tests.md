# Samples, Tests, and Conclusion

[Back: Advanced Topics](advanced-topics.md) | [Back to main README](../../README.md)

## Samples & Tests

Recommended reading order:

1. **PinionCore.Samples.HelloWorld.Protocols**
   - Basic Protocol definitions & `ProtocolCreator` usage

2. **PinionCore.Samples.HelloWorld.Server**
   - How `Entry`, `Greeter`, and `Host` are wired together

3. **PinionCore.Samples.HelloWorld.Client**
   - Proxy, ConnectingEndpoint, QueryNotifier, and basic client loop

4. **PinionCore.Integration.Tests/SampleTests.cs** (Highly Recommended)
   - Launches TCP / WebSocket / Standalone simultaneously
   - Demonstrates Rx usage (`SupplyEvent`, `RemoteValue`)
   - Shows why background loops are necessary
   - Verifies consistent behavior across all transports

5. **PinionCore.Remote.Gateway + PinionCore.Consoles.Chat1.\***
   - Real-world usage of Gateway with multiple services

---

## Conclusion

The goal of PinionCore Remote is to make remote communication *interface-driven*, removing the burden of:

- packet formatting
- serialization details
- ID / routing / session management

You can focus on domain models and state flow, while the framework handles:

- connection lifecycle
- supply / unsupply events
- version checking
- multi-transport handling
- (optionally) routing through Gateway

If you're new to this project, it is recommended to:

1. Follow the **Quick Start** to build *Protocol + Server + Client* and run Hello World.
2. Study `PinionCore.Integration.Tests` (especially `SampleTests`).
3. Explore advanced topics and code files only as needed.

If you encounter unclear documentation, missing examples, or have special use cases, feel free to open an issue or submit a PR on GitHub.

Contributions with:

- clearer explanations
- better wording
- small samples
- integration tests

are all welcomed.

Hopefully, PinionCore Remote helps you spend less time on networking details and more time on building your actual game or application.
