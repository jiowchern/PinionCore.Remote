using System;
using System.Buffers.Binary;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PinionCore.Memorys;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway.Tests
{
    /// <summary>
    /// ServiceRegistry 和 SessionListener 整合測試
    /// </summary>
    public class ServiceRegistrySessionListenerIntegrationTests
    {
        [Test, Timeout(10000)]
        public async Task ServiceRegistryAndSessionListenerExchangeMessages()
        {
            // 初始化測試組件
            var serializer = GatewayTestHelper.CreateSerializer();
            using var streamSetup = GatewayTestHelper.CreateStreamSetup();

            // 創建核心組件
            using var sessionListener = new SessionListener(streamSetup.ClientStream, PoolProvider.Shared, serializer);
            using var registry = new ServiceRegistry(PoolProvider.Shared, serializer);
            using var userProvider = new UserProvider(sessionListener, PoolProvider.Shared);
            using var subscriber = new ListenableSubscriber(sessionListener, PoolProvider.Shared);

            // 創建事件等待器
            using var eventWaiters = new GatewayTestHelper.EventWaiters();

            // 設置初始負載
            var initialPayload = new byte[] { 9, 8, 7 };

            // 設置用戶提供者事件處理（加入時發送初始消息）
            GatewayTestHelper.SetupUserProviderEvents(userProvider, eventWaiters, initialPayload);

            // 設置訂閱者回音消息處理
            GatewayTestHelper.SetupEchoMessageHandler(subscriber, eventWaiters);

            // 啟動會話監聽器並註冊服務
            sessionListener.Start();
            registry.Register(GatewayTestHelper.DefaultGroup, streamSetup.GatewayStream);

            // 創建客戶端流設置
            using var clientSetup = GatewayTestHelper.CreateStreamSetup();

            // 創建用戶會話並加入註冊表
            var session = GatewayTestHelper.CreateUserSession(99, clientSetup.GatewayStream);
            registry.Join(session);

            // 等待加入事件
            GatewayTestHelper.WaitForEvent(eventWaiters.JoinEvent, description: "加入事件");

            // 驗證收到初始消息
            var initialBuffers = await clientSetup.Reader.Read();
            Assert.That(initialBuffers.Count, Is.GreaterThan(0), "應收到至少一個初始緩衝區");
            var initialMessages = initialBuffers.Select(GatewayTestHelper.ReadMessage).ToList();
            Assert.That(initialMessages.Any(m => m.Group == GatewayTestHelper.DefaultGroup &&
                GatewayTestHelper.EndsWithPayload(m.Payload, initialPayload)), Is.True,
                "應收到包含初始負載的消息");

            // 從客戶端發送消息
            var clientPayload = new byte[] { 1, 2, 3, 4 };
            var clientBuffer = GatewayTestHelper.CreateGroupBuffer(GatewayTestHelper.DefaultGroup, clientPayload);
            clientSetup.Sender.Push(clientBuffer);

            // 等待服務端處理消息
            GatewayTestHelper.WaitForEvent(eventWaiters.MessageEvent, description: "服務端消息處理事件");

            // 驗證收到回音消息
            var responseBuffers = await clientSetup.Reader.Read();
            Assert.That(responseBuffers.Count, Is.GreaterThan(0), "應收到至少一個回應緩衝區");
            var responseMessages = responseBuffers.Select(GatewayTestHelper.ReadMessage).ToList();
            Assert.That(responseMessages.Any(m => m.Group == GatewayTestHelper.DefaultGroup &&
                m.Payload.Length >= clientPayload.Length), Is.True,
                "應收到包含客戶端負載的回音消息");

            // 測試離開流程
            registry.Leave(session);
            GatewayTestHelper.WaitForEvent(eventWaiters.LeaveEvent, description: "離開事件");
        }
    }
}
