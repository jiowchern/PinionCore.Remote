using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;
using NUnit.Framework;
using PinionCore.Remote.Client;
using PinionCore.Remote.Server;
using PinionCore.Remote.Reactive;
using System.Linq;

namespace PinionCore.Integration.Tests
{
    /// <summary>
    /// Integration tests demonstrating PinionCore.Remote's support for multiple transport modes:
    /// TCP, WebSocket, and Standalone (in-memory). These tests verify that all three transport
    /// modes work identically, allowing seamless switching between network and local execution.
    /// </summary>
    public class SampleTests
    {
        /// <summary>
        /// Demonstrates concurrent testing of TCP, WebSocket, and Standalone transports.
        /// All three clients connect to the same server instance and execute identical echo tests,
        /// proving transport-agnostic behavior.
        /// </summary>
        [Test,Timeout(5000)]
        public async Task SampleTest()
        {
            // ========================================
            // Step 1: Setup server-side components
            // ========================================

            // Create the echo service implementation (returns the value passed to it)
            var echo = new PinionCore.Remote.Tools.Protocol.Sources.TestCommon.EchoTester(1);

            // Create a mock entry that binds the echo service when clients connect
            var entry = NSubstitute.Substitute.For<PinionCore.Remote.IEntry>();
            entry.OnSessionOpened(CreateEchoBinder(echo));

            // Create the protocol (defines serialization and interface contracts)
            var protocol = PinionCore.Remote.Tools.Protocol.Sources.TestCommon.ProtocolProvider.CreateCase1();

            // ========================================
            // Step 2: Allocate ports and create server
            // ========================================

            // Get available ports for TCP and WebSocket (avoid port conflicts)
            var ports = PinionCore.Network.Tcp.Tools.GetAvailablePorts(2).ToArray();
            var tcpPort = ports[0];
            var webPort = ports[1];

            // Create the server host
            var host = new PinionCore.Remote.Server.Host(entry, protocol);

            // Create Standalone endpoint (in-memory, no network required)
            var standaloneListener = new PinionCore.Remote.Standalone.ListeningEndpoint();

            // Start listening on all three transport modes simultaneously
            var (disposeServer, errorInfos) = await host.ListenAsync(
                new PinionCore.Remote.Server.Tcp.ListeningEndpoint(tcpPort, 10),
                new PinionCore.Remote.Server.Web.ListeningEndpoint($"http://localhost:{webPort}/"),
                standaloneListener
            );

            // Fail the test if any listener failed to start
            foreach (var err in errorInfos)
            {
                Assert.Fail($"Server error: {err.Exception}");
            }

            // ========================================
            // Step 3: Run concurrent client tests
            // ========================================

            try
            {
                // TCP client test (network-based)
                var tcpTask = Task.Run(async () =>
                {
                    var tcpConnector = new PinionCore.Remote.Client.Tcp.ConnectingEndpoint(new IPEndPoint(IPAddress.Loopback, tcpPort));
                    var tcpProxy = new PinionCore.Remote.Client.Proxy(protocol);
                    var tcpDisposeClient = await tcpProxy.Connect(tcpConnector);
                    await RunGhostEchoTestAsync(tcpProxy);
                    tcpDisposeClient.Dispose();
                });

                // WebSocket client test (web-based)
                var webTask = Task.Run(async () =>
                {
                    var webConnector = new PinionCore.Remote.Client.Web.ConnectingEndpoint($"ws://localhost:{webPort}/");
                    var webProxy = new PinionCore.Remote.Client.Proxy(protocol);
                    var webDisposeClient = await webProxy.Connect(webConnector);
                    await RunGhostEchoTestAsync(webProxy);
                    webDisposeClient.Dispose();
                });

                // Standalone client test (in-memory, no network)
                var standaloneTask = Task.Run(async () =>
                {
                    var standaloneProxy = new PinionCore.Remote.Client.Proxy(protocol);
                    var standaloneDisposeClient = await standaloneProxy.Connect(standaloneListener);
                    await RunGhostEchoTestAsync(standaloneProxy);
                    standaloneDisposeClient.Dispose();
                });

                // Wait for all three transport modes to complete their tests
                Task.WaitAll(tcpTask, webTask, standaloneTask);
            }
            catch
            {
                throw;
            }

            // ========================================
            // Step 4: Cleanup server resources
            // ========================================
            disposeServer.Dispose();
        }

        /// <summary>
        /// Executes the echo test using Reactive Extensions (Rx) to demonstrate
        /// asynchronous remote method invocation with event-driven interface discovery.
        ///
        /// Key concepts demonstrated:
        /// 1. Background processing loop (HandlePackets/HandleMessages) is required for remote events
        /// 2. SupplyEvent() converts Notifier events to IObservable
        /// 3. RemoteValue() converts remote method results to IObservable
        /// 4. LINQ query syntax elegantly chains asynchronous remote operations
        /// </summary>
        /// <param name="proxy">The client proxy instance</param>
        private static async Task RunGhostEchoTestAsync(Proxy proxy)
        {
            CancellationTokenSource? cts = null;
            Task? runTask = null;

            // ========================================
            // Step 1: Start background processing loop
            // ========================================
            // IMPORTANT: HandlePackets() and HandleMessages() must run continuously
            // in the background, otherwise remote events won't be processed
            cts = new CancellationTokenSource();
            runTask = Task.Run(CreateGhostProcessingLoop(proxy, cts), cts.Token);

            // ========================================
            // Step 2: Execute remote call using Rx
            // ========================================
            // Build an observable chain that:
            // 1. Waits for the Echoable interface to be supplied by the server (SupplyEvent)
            // 2. Calls the Echo() method and converts the remote result to IObservable (RemoteValue)
            // 3. Selects the final value
            var echoObs = from e in proxy.Agent.QueryNotifier<PinionCore.Remote.Tools.Protocol.Sources.TestCommon.Echoable>().SupplyEvent()
                          from val in e.Echo().RemoteValue()
                          select val;

            // Await the first (and only) echo result
            var echoValue = await echoObs.FirstAsync();
            Assert.AreEqual(1, echoValue);

            // ========================================
            // Step 3: Stop background processing
            // ========================================
            cts.Cancel();
            await runTask;
        }

        /// <summary>
        /// Creates a mock session binder callback for NSubstitute.
        /// This is called when a client connects to the server.
        /// </summary>
        private static Remote.ISessionBinder CreateEchoBinder(Remote.Tools.Protocol.Sources.TestCommon.EchoTester echo)
        {
            return NSubstitute.Arg.Do<PinionCore.Remote.ISessionBinder>(b => BindEchoable(echo, b));
        }

        /// <summary>
        /// Binds the echo service implementation to the client session.
        /// This makes the Echoable interface available to the connected client.
        /// </summary>
        private static Remote.ISoul BindEchoable(Remote.Tools.Protocol.Sources.TestCommon.EchoTester echo, Remote.ISessionBinder b)
        {
            return b.Bind<PinionCore.Remote.Tools.Protocol.Sources.TestCommon.Echoable>(echo);
        }

        /// <summary>
        /// Creates a background processing loop for the client proxy.
        ///
        /// CRITICAL: This loop is REQUIRED for PinionCore.Remote to function.
        /// Without continuous calls to HandlePackets() and HandleMessages(),
        /// the client will not receive events or remote method results.
        ///
        /// Why this is needed:
        /// - HandlePackets(): Processes incoming network packets and decodes them
        /// - HandleMessages(): Dispatches decoded messages to event handlers
        /// - These must be called repeatedly (typically in a background thread or update loop)
        /// </summary>
        /// <param name="proxy">The client proxy to process</param>
        /// <param name="cts">Cancellation token to stop the loop</param>
        /// <returns>An async function that runs the processing loop</returns>
        private static System.Func<Task> CreateGhostProcessingLoop(Proxy proxy, CancellationTokenSource cts)
        {
            return async () =>
            {
                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        // Process incoming packets from the server
                        proxy.Agent.HandlePackets();

                        // Process decoded messages and trigger events
                        proxy.Agent.HandleMessages();

                        // Small delay to prevent busy-waiting (CPU optimization)
                        await Task.Delay(1, cts.Token);
                    }
                }
                catch (System.OperationCanceledException) when (cts.IsCancellationRequested)
                {
                    // Expected cancellation when test completes - treat as normal termination
                }
            };
        }
    }
}
