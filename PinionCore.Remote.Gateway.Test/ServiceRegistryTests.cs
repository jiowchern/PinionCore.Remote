using System;
using System.Buffers.Binary;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using PinionCore.Memorys;

namespace PinionCore.Remote.Gateway.Tests
{
    /// <summary>
    /// ServiceRegistry 功能測試
    /// </summary>
    public class ServiceRegistryTests
    {
        [Test]
        public async Task JoinRoutesMessagesBothWays()
        {
            // 初始化測試組件
            var serializer = GatewayTestHelper.CreateSerializer();
            using var registry = new ServiceRegistry(PoolProvider.Shared, serializer);

            // 創建服務端流設置
            using var serviceSetup = GatewayTestHelper.CreateStreamSetup();
            registry.Register(GatewayTestHelper.DefaultGroup, serviceSetup.GatewayStream);

            // 創建客戶端流設置
            using var clientSetup = GatewayTestHelper.CreateStreamSetup();

            // 創建用戶會話並加入註冊表
            var session = GatewayTestHelper.CreateUserSession(42, clientSetup.GatewayStream);
            registry.Join(session);

            // 驗證加入消息
            var joinPackage = await GatewayTestHelper.ReadAndDeserialize<ServiceRegistryPackage>(
                serviceSetup.Reader, serializer);
            Assert.That(joinPackage.OpCode, Is.EqualTo(OpCodeFromServiceRegistry.Join),
                "加入包的操作碼應為 Join");
            Assert.That(joinPackage.UserId, Is.EqualTo(session.Id),
                "加入包的用戶 ID 應與會話 ID 一致");

            // 從服務端發送消息到客戶端
            var servicePayload = new byte[] { 0, 10, 20, 30 };
            var servicePackage = GatewayTestHelper.CreateSessionListenerPackage(
                OpCodeFromSessionListener.Message, session.Id, servicePayload);
            GatewayTestHelper.SerializeAndSend(servicePackage, serializer, serviceSetup.Sender);

            // 驗證客戶端收到的消息
            var userBuffers = await clientSetup.Reader.Read();
            Assert.That(userBuffers.Count, Is.GreaterThan(0), "客戶端應收到至少一個緩衝區");
            var userMessage = GatewayTestHelper.ReadMessage(userBuffers[0]);
            Assert.That(userMessage.Group, Is.EqualTo(GatewayTestHelper.DefaultGroup),
                "消息群組應為預設群組");
            CollectionAssert.AreEqual(servicePayload, userMessage.Payload, "負載內容應相同");

            // 從客戶端發送消息到服務端
            var clientPayload = new byte[] { 5, 4, 3, 2, 1 };
            var clientBuffer = GatewayTestHelper.CreateGroupBuffer(GatewayTestHelper.DefaultGroup, clientPayload);
            clientSetup.Sender.Push(clientBuffer);

            // 驗證服務端收到的消息
            var messagePackage = await GatewayTestHelper.ReadAndDeserialize<ServiceRegistryPackage>(
                serviceSetup.Reader, serializer);
            Assert.That(messagePackage.OpCode, Is.EqualTo(OpCodeFromServiceRegistry.Message),
                "消息包的操作碼應為 Message");
            Assert.That(messagePackage.UserId, Is.EqualTo(session.Id),
                "消息包的用戶 ID 應與會話 ID 一致");
            CollectionAssert.AreEqual(clientPayload, messagePackage.Payload, "負載內容應相同");

            // 測試離開流程
            registry.Leave(session);

            // 驗證離開消息
            var leavePackage = await GatewayTestHelper.ReadAndDeserialize<ServiceRegistryPackage>(
                serviceSetup.Reader, serializer);
            Assert.That(leavePackage.OpCode, Is.EqualTo(OpCodeFromServiceRegistry.Leave),
                "離開包的操作碼應為 Leave");
            Assert.That(leavePackage.UserId, Is.EqualTo(session.Id),
                "離開包的用戶 ID 應與會話 ID 一致");
        }

        [Test]
        public async Task RegisterAfterUserJoinBindsExistingSession()
        {
            // 測試先加入用戶後註冊服務的情況
            var serializer = GatewayTestHelper.CreateSerializer();
            using var registry = new ServiceRegistry(PoolProvider.Shared, serializer);

            // 創建客戶端會話並先加入註冊表
            using var clientSetup = GatewayTestHelper.CreateStreamSetup();
            var session = GatewayTestHelper.CreateUserSession(7, clientSetup.GatewayStream);
            registry.Join(session);

            // 後註冊服務端
            using var serviceSetup = GatewayTestHelper.CreateStreamSetup();
            registry.Register(GatewayTestHelper.DefaultGroup, serviceSetup.GatewayStream);

            // 驗證服務端收到現有會話的加入消息
            var joinPackage = await GatewayTestHelper.ReadAndDeserialize<ServiceRegistryPackage>(
                serviceSetup.Reader, serializer);
            Assert.That(joinPackage.OpCode, Is.EqualTo(OpCodeFromServiceRegistry.Join),
                "應收到加入包");
            Assert.That(joinPackage.UserId, Is.EqualTo(session.Id),
                "加入包的用戶 ID 應與現有會話 ID 一致");
        }

        [Test]
        public async Task UnregisterReassignsUserToRemainingService()
        {
            // 測試註銷服務時用戶重新分配到剩餘服務的情況
            var serializer = GatewayTestHelper.CreateSerializer();
            using var registry = new ServiceRegistry(PoolProvider.Shared, serializer);

            // 註冊第一個服務 A
            using var serviceSetupA = GatewayTestHelper.CreateStreamSetup();
            registry.Register(GatewayTestHelper.DefaultGroup, serviceSetupA.GatewayStream);

            // 創建用戶會話並加入註冊表
            using var sessionSetup = GatewayTestHelper.CreateStreamSetup();
            var session = GatewayTestHelper.CreateUserSession(9, sessionSetup.GatewayStream);
            registry.Join(session);

            // 驗證服務 A 收到初始加入消息
            var initialJoin = await serviceSetupA.Reader.Read();
            Assert.That(initialJoin.Count, Is.GreaterThan(0), "服務 A 應收到初始加入消息");

            // 註冊第二個服務 B
            using var serviceSetupB = GatewayTestHelper.CreateStreamSetup();
            registry.Register(GatewayTestHelper.DefaultGroup, serviceSetupB.GatewayStream);

            // 註銷服務 A
            registry.Unregister(GatewayTestHelper.DefaultGroup, serviceSetupA.GatewayStream);

            // 驗證用戶被重新分配到服務 B
            var reassignedPackage = await GatewayTestHelper.ReadAndDeserialize<ServiceRegistryPackage>(
                serviceSetupB.Reader, serializer);
            Assert.That(reassignedPackage.OpCode, Is.EqualTo(OpCodeFromServiceRegistry.Join),
                "用戶應被重新分配到服務 B（收到加入包）");
            Assert.That(reassignedPackage.UserId, Is.EqualTo(session.Id),
                "重新分配的用戶 ID 應與原會話 ID 一致");
        }
    }
}
