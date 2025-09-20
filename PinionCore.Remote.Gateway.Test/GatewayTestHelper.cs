using System;
using System.Buffers.Binary;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PinionCore.Memorys;
using PinionCore.Network;
using PinionCore.Remote.Soul;

namespace PinionCore.Remote.Gateway.Tests
{
    /// <summary>
    /// Gateway 測試輔助類，提供通用的測試設置和工具方法
    /// </summary>
    public static class GatewayTestHelper
    {
        /// <summary>
        /// 預設群組 ID
        /// </summary>
        public const uint DefaultGroup = 1;

        /// <summary>
        /// 預設超時時間（秒）
        /// </summary>
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// 網路流設置結果
        /// </summary>
        public class StreamSetup : IDisposable
        {
            public Stream ClientStream { get; }
            public ReverseStream GatewayStream { get; }
            public PackageReader Reader { get; }
            public PackageSender Sender { get; }

            public StreamSetup(Stream clientStream, ReverseStream gatewayStream, PackageReader reader, PackageSender sender)
            {
                ClientStream = clientStream;
                GatewayStream = gatewayStream;
                Reader = reader;
                Sender = sender;
            }

            public void Dispose()
            {
                // 這些網路類型不實作 IDisposable，所以無需 Dispose
                // 保留此方法以符合 IDisposable 模式
            }
        }

        /// <summary>
        /// 事件等待器集合
        /// </summary>
        public class EventWaiters : IDisposable
        {
            public ManualResetEventSlim JoinEvent { get; } = new ManualResetEventSlim(false);
            public ManualResetEventSlim LeaveEvent { get; } = new ManualResetEventSlim(false);
            public ManualResetEventSlim MessageEvent { get; } = new ManualResetEventSlim(false);

            public void Dispose()
            {
                JoinEvent?.Dispose();
                LeaveEvent?.Dispose();
                MessageEvent?.Dispose();
            }
        }

        /// <summary>
        /// 創建序列化器
        /// </summary>
        /// <returns>ServiceRegistrySessionListenerSerializer 實例</returns>
        internal static ServiceRegistrySessionListenerSerializer CreateSerializer()
        {
            return new ServiceRegistrySessionListenerSerializer(PoolProvider.Shared);
        }

        /// <summary>
        /// 創建雙向網路流設置
        /// </summary>
        /// <returns>包含客戶端流和網關流的設置對象</returns>
        public static StreamSetup CreateStreamSetup()
        {
            var clientStream = new Stream();
            var gatewayStream = new ReverseStream(clientStream);
            var reader = new PackageReader(clientStream, PoolProvider.Shared);
            var sender = new PackageSender(clientStream, PoolProvider.Shared);

            return new StreamSetup(clientStream, gatewayStream, reader, sender);
        }

        /// <summary>
        /// 創建用戶會話
        /// </summary>
        /// <param name="userId">用戶 ID</param>
        /// <param name="gatewayStream">網關流</param>
        /// <returns>用戶會話實例</returns>
        internal static UserSession CreateUserSession(uint userId, ReverseStream gatewayStream)
        {
            return new UserSession(userId, gatewayStream, PoolProvider.Shared);
        }

        /// <summary>
        /// 創建 ServiceRegistry 包
        /// </summary>
        /// <param name="opCode">操作碼</param>
        /// <param name="userId">用戶 ID</param>
        /// <param name="payload">負載數據（可選）</param>
        /// <returns>ServiceRegistry 包實例</returns>
        internal static ServiceRegistryPackage CreateServiceRegistryPackage(OpCodeFromServiceRegistry opCode, uint userId, byte[] payload = null)
        {
            return new ServiceRegistryPackage
            {
                OpCode = opCode,
                UserId = userId,
                Payload = payload ?? new byte[0]
            };
        }

        /// <summary>
        /// 創建 SessionListener 包
        /// </summary>
        /// <param name="opCode">操作碼</param>
        /// <param name="userId">用戶 ID</param>
        /// <param name="payload">負載數據（可選）</param>
        /// <returns>SessionListener 包實例</returns>
        internal static SessionListenerPackage CreateSessionListenerPackage(OpCodeFromSessionListener opCode, uint userId, byte[] payload = null)
        {
            return new SessionListenerPackage
            {
                OpCode = opCode,
                UserId = userId,
                Payload = payload ?? new byte[0]
            };
        }

        /// <summary>
        /// 創建包含群組 ID 和負載的緩衝區
        /// </summary>
        /// <param name="group">群組 ID</param>
        /// <param name="payload">負載數據</param>
        /// <returns>包含群組 ID 和負載的緩衝區</returns>
        public static Memorys.Buffer CreateGroupBuffer(uint group, byte[] payload)
        {
            var buffer = PoolProvider.Shared.Alloc(payload.Length + sizeof(uint));
            var segment = buffer.Bytes;

            // 寫入群組 ID（小端序）
            BinaryPrimitives.WriteUInt32LittleEndian(
                segment.Array.AsSpan(segment.Offset, sizeof(uint)), group);

            // 複製負載數據
            Array.Copy(payload, 0, segment.Array, segment.Offset + sizeof(uint), payload.Length);

            return buffer;
        }

        /// <summary>
        /// 從緩衝區讀取消息（群組 ID 和負載）
        /// </summary>
        /// <param name="buffer">輸入緩衝區</param>
        /// <returns>包含群組 ID 和負載的元組</returns>
        public static (uint Group, byte[] Payload) ReadMessage(Memorys.Buffer buffer)
        {
            var segment = buffer.Bytes;
            if (segment.Count < sizeof(uint))
            {
                return (0, Array.Empty<byte>());
            }

            // 讀取群組 ID（小端序）
            var group = BinaryPrimitives.ReadUInt32LittleEndian(
                segment.Array.AsSpan(segment.Offset, sizeof(uint)));

            // 提取負載數據
            var payload = new byte[segment.Count - sizeof(uint)];
            if (payload.Length > 0)
            {
                Array.Copy(segment.Array, segment.Offset + sizeof(uint), payload, 0, payload.Length);
            }

            return (group, payload);
        }

        /// <summary>
        /// 序列化包並發送
        /// </summary>
        /// <param name="package">要序列化的包</param>
        /// <param name="serializer">序列化器</param>
        /// <param name="sender">包發送器</param>
        internal static void SerializeAndSend(object package, ServiceRegistrySessionListenerSerializer serializer, PackageSender sender)
        {
            var buffer = serializer.Serialize(package);
            sender.Push(buffer);
        }

        /// <summary>
        /// 讀取並反序列化包
        /// </summary>
        /// <typeparam name="T">期望的包類型</typeparam>
        /// <param name="reader">包讀取器</param>
        /// <param name="serializer">序列化器</param>
        /// <returns>反序列化的包</returns>
        internal static async Task<T> ReadAndDeserialize<T>(PackageReader reader, ServiceRegistrySessionListenerSerializer serializer)
        {
            var buffers = await reader.Read();
            Assert.That(buffers.Count, Is.GreaterThan(0), "應該至少收到一個緩衝區");
            return (T)serializer.Deserialize(buffers[0]);
        }

        /// <summary>
        /// 等待事件觸發
        /// </summary>
        /// <param name="eventSlim">要等待的事件</param>
        /// <param name="timeout">超時時間（可選，預設使用 DefaultTimeout）</param>
        /// <param name="description">事件描述（用於斷言失敗訊息）</param>
        public static void WaitForEvent(ManualResetEventSlim eventSlim, TimeSpan? timeout = null, string description = "事件")
        {
            var actualTimeout = timeout ?? DefaultTimeout;
            Assert.That(eventSlim.Wait(actualTimeout), Is.True, $"{description}應該在 {actualTimeout.TotalSeconds} 秒內觸發");
        }

        /// <summary>
        /// 檢查負載是否以指定的字節序列結尾
        /// </summary>
        /// <param name="actual">實際的字節陣列</param>
        /// <param name="expected">期望的結尾字節序列</param>
        /// <returns>如果實際陣列以期望序列結尾則返回 true</returns>
        public static bool EndsWithPayload(byte[] actual, byte[] expected)
        {
            if (actual == null || actual.Length < expected.Length)
            {
                return false;
            }

            for (var i = 0; i < expected.Length; i++)
            {
                if (actual[actual.Length - expected.Length + i] != expected[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 反轉字節陣列
        /// </summary>
        /// <param name="source">源字節陣列</param>
        /// <returns>反轉後的緩衝區</returns>
        public static Memorys.Buffer ReverseBytes(byte[] source)
        {
            var reversedBuffer = PoolProvider.Shared.Alloc(source.Length);
            for (int i = 0; i < source.Length; i++)
            {
                reversedBuffer[i] = source[source.Length - 1 - i];
            }
            return reversedBuffer;
        }

        /// <summary>
        /// 設置 UserProvider 的標準事件處理器
        /// </summary>
        /// <param name="userProvider">用戶提供者</param>
        /// <param name="eventWaiters">事件等待器</param>
        /// <param name="initialPayload">加入時發送的初始負載（可選）</param>
        /// <param name="group">群組 ID（可選，預設使用 DefaultGroup）</param>
        public static void SetupUserProviderEvents(UserProvider userProvider, EventWaiters eventWaiters,
            byte[] initialPayload = null, uint group = DefaultGroup)
        {
            userProvider.JoinEvent += (_, reader, sender) =>
            {
                if (initialPayload != null)
                {
                    var buffer = CreateGroupBuffer(group, initialPayload);
                    sender.Push(buffer);
                }
                eventWaiters.JoinEvent.Set();
            };

            userProvider.LeaveEvent += _ => eventWaiters.LeaveEvent.Set();
        }

        /// <summary>
        /// 設置 ListenableSubscriber 的回音消息處理器
        /// </summary>
        /// <param name="subscriber">可監聽訂閱者</param>
        /// <param name="eventWaiters">事件等待器</param>
        public static void SetupEchoMessageHandler(ListenableSubscriber subscriber, EventWaiters eventWaiters)
        {
            subscriber.MessageEvent += (_, buffers, sender) =>
            {
                eventWaiters.MessageEvent.Set();
                foreach (var buffer in buffers)
                {
                    var message = ReadMessage(buffer);
                    var echoBuffer = CreateGroupBuffer(message.Group, message.Payload);
                    sender.Push(echoBuffer);
                }
            };
        }
    }
}
