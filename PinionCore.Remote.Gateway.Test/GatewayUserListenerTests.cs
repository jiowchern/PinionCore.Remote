using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using PinionCore.Memorys;
using PinionCore.Remote.Gateway;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway.Tests
{
    /// <summary>
    /// GatewayUserListener 功能測試
    /// </summary>
    public class GatewayUserListenerTests
    {
        [Test, Timeout(10000)]
        public async System.Threading.Tasks.Task Test()
        {
            // 創建基礎流（注意：GatewayUserListener 和 網關讀寫器使用相反的流）
            var stream = new PinionCore.Network.Stream();
            var gatewayStream = new PinionCore.Network.ReverseStream(stream);
            var gatewayReader = new PinionCore.Network.PackageReader(gatewayStream, PoolProvider.Shared);
            var gatewayWriter = new PinionCore.Network.PackageSender(gatewayStream, PoolProvider.Shared);

            var serializer = GatewayTestHelper.CreateSerializer();

            // 創建 GatewayUserListener 和相關組件（GatewayUserListener 使用原始流）
            var sessionListener = new GatewayUserListener(stream, PoolProvider.Shared, serializer);
            var userProvider = new UserProvider(sessionListener, PoolProvider.Shared);

            // 創建事件等待器
            using var eventWaiters = new GatewayTestHelper.EventWaiters();

            // 設置用戶提供者事件處理
            userProvider.LeaveEvent += (_) => {
                Console.WriteLine("LeaveEvent triggered");
                eventWaiters.LeaveEvent.Set();
            };

            userProvider.JoinEvent += (_,_,_) => {
                Console.WriteLine("JoinEvent triggered");
                eventWaiters.JoinEvent.Set();
            };

            // 創建可監聽訂閱者並設置消息反轉處理
            var listenableSubscriber = new ListenableSubscriber(sessionListener, PoolProvider.Shared);
            listenableSubscriber.MessageEvent += (stream, buffers, sender) =>
            {
                Console.WriteLine($"ListenableSubscriber.MessageEvent: 收到 {buffers.Count()} 個緩衝區");

                // 反轉緩衝區數據並發送回去
                foreach(var buffer in buffers)
                {
                    Console.WriteLine($"原始緩衝區: [{string.Join(", ", buffer.Bytes.ToArray())}]");

                    // 反轉緩衝區數據
                    var revertBuf = PoolProvider.Shared.Alloc(buffer.Bytes.Count);
                    for(int i = 0; i < buffer.Bytes.Count; i++)
                    {
                        revertBuf[i] = buffer.Bytes[buffer.Bytes.Count - 1 - i];
                    }
                    Console.WriteLine($"反轉後緩衝區: [{string.Join(", ", revertBuf.Bytes.ToArray())}]");
                    sender.Push(revertBuf);
                    Console.WriteLine("緩衝區已發送回發送者");
                }
            };

            // 設置斷線事件處理
            sessionListener.OnDisconnected += () =>
            {
                Console.WriteLine("GatewayUserListener 斷線");
            };

            // 啟動 GatewayUserListener
            Console.WriteLine("啟動 GatewayUserListener");
            sessionListener.Start();

            // 測試加入流程
            Console.WriteLine("發送加入包");
            var joinPackage = GatewayTestHelper.CreateServiceRegistryPackage(
                OpCodeFromServiceRegistry.Join, 1);
            GatewayTestHelper.SerializeAndSend(joinPackage, serializer, gatewayWriter);

            Console.WriteLine("等待加入事件");
            GatewayTestHelper.WaitForEvent(eventWaiters.JoinEvent, description: "加入事件");
            Console.WriteLine("成功收到加入事件");

            // 測試消息處理流程
            Console.WriteLine("發送消息包");
            var messagePackage = GatewayTestHelper.CreateServiceRegistryPackage(
                OpCodeFromServiceRegistry.Message, 1,
                new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            GatewayTestHelper.SerializeAndSend(messagePackage, serializer, gatewayWriter);

            // 讀取並驗證回應
            Console.WriteLine("讀取回應");
            var responsePackage = await GatewayTestHelper.ReadAndDeserialize<SessionListenerPackage>(
                gatewayReader, serializer);

            Console.WriteLine($"回應 OpCode: {responsePackage.OpCode}, UserId: {responsePackage.UserId}, 負載長度: {responsePackage.Payload?.Length ?? 0}");

            // 驗證回應內容
            Assert.That(responsePackage.OpCode, Is.EqualTo(OpCodeFromSessionListener.Message),
                "回應操作碼應為 Message");
            Assert.That(responsePackage.UserId, Is.EqualTo(1), "回應用戶 ID 應為 1");
            Assert.IsTrue(responsePackage.Payload.SequenceEqual(new byte[] { 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 }),
                "負載應為反轉後的數據");
            Console.WriteLine("消息測試通過");

            // 測試離開流程
            Console.WriteLine("發送離開包");
            var leavePackage = GatewayTestHelper.CreateServiceRegistryPackage(
                OpCodeFromServiceRegistry.Leave, 1);
            GatewayTestHelper.SerializeAndSend(leavePackage, serializer, gatewayWriter);

            Console.WriteLine("等待離開事件");
            GatewayTestHelper.WaitForEvent(eventWaiters.LeaveEvent, description: "離開事件");
            Console.WriteLine("成功收到離開事件");
        }
    }
}

