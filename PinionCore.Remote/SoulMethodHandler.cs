using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PinionCore.Extensions;
using PinionCore.Memorys;
using PinionCore.Remote.Packages;
using PinionCore.Utility;

namespace PinionCore.Remote
{
    // MethodHandler.cs
    public class SoulMethodHandler
    {
        private readonly IRequestQueue _Peer;
        private readonly IResponseQueue _Queue;
        private readonly IProtocol _Protocol;
        private readonly ConcurrentDictionary<long, SoulProxy> _Souls;
        private readonly Dictionary<long, IValue> _WaitValues;
        private readonly ISerializable _Serializer;
        private readonly IInternalSerializable _InternalSerializable;
        private readonly SoulBindHandler _BindHandler;
        private readonly List<KeyValuePair<ISpiritSoul, Action>> _SpiritDisposeHandlers;

        internal SoulMethodHandler(
            IRequestQueue peer,
            IResponseQueue queue,
            IProtocol protocol,
            ConcurrentDictionary<long, SoulProxy> souls,

            ISerializable serializer,
            IInternalSerializable internalSerializable,
            SoulBindHandler bindHandler)
        {
            _Peer = peer;
            _Queue = queue;
            _Protocol = protocol;
            _Souls = souls;
            _WaitValues = new Dictionary<long, IValue>();
            _Serializer = serializer;
            _InternalSerializable = internalSerializable;
            _BindHandler = bindHandler;
            _SpiritDisposeHandlers = new List<KeyValuePair<ISpiritSoul, Action>>();

            _Peer.InvokeMethodEvent += _InvokeMethod;
            _Peer.InvokeStreamMethodEvent += _InvokeStreamMethod;
        }

        public void Dispose()
        {
            _Peer.InvokeMethodEvent -= _InvokeMethod;
            _Peer.InvokeStreamMethodEvent -= _InvokeStreamMethod;

            // session 結束：解除所有 Spirit 的 Dispose 訂閱，避免 Spirit 存活超過 session 造成洩漏
            KeyValuePair<ISpiritSoul, Action>[] handlers;
            lock (_SpiritDisposeHandlers)
            {
                handlers = _SpiritDisposeHandlers.ToArray();
                _SpiritDisposeHandlers.Clear();
            }
            foreach (KeyValuePair<ISpiritSoul, Action> handler in handlers)
            {
                handler.Key.DisposeEvent -= handler.Value;
            }
        }

        private void _InvokeMethod(long entity_id, int method_id, long returnId, byte[][] args)
        {
            SoulProxy soul;
            if (!_Souls.TryGetValue(entity_id, out soul))
            {
                // soul 已解綁而請求仍在途中屬正常競態:回錯誤讓 client 的 Value 得到結果,不能丟例外
                var message = $"Soul not found entity_id:{entity_id}";
                Log.Instance.WriteInfo(message);
                _ErrorDeserialize(method_id.ToString(), returnId, message);
                return;
            }


            var soulInfo = new
            {
                soul.MethodInfos,
                soul.ObjectInstance
            };

            MethodInfo info = _Protocol.GetMemberMap().GetMethod(method_id);
            MethodInfo methodInfo =
                (from m in soulInfo.MethodInfos where m == info && m.GetParameters().Count() == args.Count() select m)
                    .FirstOrDefault();
            if (methodInfo == null)
            {
                var message = $"Method not found method_id:{method_id} protocol:{_Protocol.VersionCode.ToMd5String()}";
                Log.Instance.WriteInfo(message);
                _ErrorDeserialize(method_id.ToString(), returnId, message);
                return;
            }

            try
            {

                IEnumerable<object> argObjects = args.Zip(methodInfo.GetParameters(), (arg, par) => _Serializer.Deserialize(par.ParameterType, arg.AsBuffer()));

                var returnValue = methodInfo.Invoke(soulInfo.ObjectInstance, argObjects.ToArray());
                if (returnValue != null)
                {
                    _ReturnValue(returnId, returnValue as IValue);
                }
            }
            catch (DeserializeException deserialize_exception)
            {
                var message = deserialize_exception.Base.ToString();
                _ErrorDeserialize(method_id.ToString(), returnId, message);
            }
            catch (Exception e)
            {
                Log.Instance.WriteDebug(e.ToString());
                _ErrorDeserialize(method_id.ToString(), returnId, e.Message);
            }
        }

        private void _ReturnValue(long returnId, IValue returnValue)
        {
            if (_WaitValues.ContainsKey(returnId))
                return;

            _WaitValues.Add(returnId, returnValue);
            returnValue.QueryValue(obj =>
            {
                if (!returnValue.IsInterface())
                {
                    _ReturnDataValue(returnId, returnValue);
                }
                else
                {
                    _ReturnSoulValue(returnId, returnValue);
                }
                _WaitValues.Remove(returnId);
            });
        }

        private void _ReturnDataValue(long returnId, IValue returnValue)
        {
            var value = returnValue.GetObject();
            var package = new PackageReturnValue
            {
                ReturnTarget = returnId,
                ReturnValue = _Serializer.Serialize(returnValue.GetObjectType(), value).ToArray()
            };
            _Queue.Push(ServerToClientOpCode.ReturnValue, _InternalSerializable.Serialize(package));
        }

        private void _ReturnSoulValue(long returnId, IValue returnValue)
        {
            var spirit = returnValue as ISpiritSoul;
            if (spirit != null && spirit.Disposed)
            {
                // 已 Dispose 的 Spirit 永不供給
                return;
            }

            SoulProxy proxy = _BindHandler.BindReturn(returnValue.GetObject(), returnValue.GetObjectType(), returnId);
            if (spirit == null)
            {
                // 一般 Value<介面> 回傳：僅綁定
                return;
            }

            Action onDispose = null;
            onDispose = () =>
            {
                spirit.DisposeEvent -= onDispose;
                lock (_SpiritDisposeHandlers)
                {
                    _SpiritDisposeHandlers.Remove(new KeyValuePair<ISpiritSoul, Action>(spirit, onDispose));
                }
                // soul 可能已由其他路徑解綁（如 client 端 Release），避免對不存在的 soul 解綁擲例外進使用者的 Dispose 呼叫
                if (_Souls.ContainsKey(proxy.Id))
                {
                    ISoul soul = proxy;
                    _BindHandler.Unbind(soul);
                }
            };
            lock (_SpiritDisposeHandlers)
            {
                _SpiritDisposeHandlers.Add(new KeyValuePair<ISpiritSoul, Action>(spirit, onDispose));
            }
            // DisposeEvent 具補發語意：綁定後才 Dispose 的競態也能解綁
            spirit.DisposeEvent += onDispose;
        }

        private void _ErrorDeserialize(string methodName, long returnId, string message)
        {
            var package = new PackageErrorMethod
            {
                Method = methodName,
                ReturnTarget = returnId,
                Message = message
            };
            _Queue.Push(ServerToClientOpCode.ErrorMethod, _InternalSerializable.Serialize(package));
        }

        private void _InvokeStreamMethod(long entity_id, int method_id, long returnId, byte[] buffer, int count)
        {
            if (!_Souls.TryGetValue(entity_id, out SoulProxy soul))
            {
                var message = $"Soul not found entity_id:{entity_id}";
                Log.Instance.WriteInfo(message);
                _ErrorDeserialize(method_id.ToString(), returnId, message);
                return;
            }

            MethodInfo info = _Protocol.GetMemberMap().GetMethod(method_id);
            if (info == null)
            {
                var message = $"Stream method not found method_id:{method_id}";
                Log.Instance.WriteInfo(message);
                _ErrorDeserialize(method_id.ToString(), returnId, message);
                return;
            }

            try
            {
                var args = new object[] { buffer, 0, count, System.Threading.CancellationToken.None };
                var returnValue = info.Invoke(soul.ObjectInstance, args);
                if (returnValue is IAwaitableSource<int> awaitable)
                {
                    _HandleStreamReturn(method_id, returnId, buffer, awaitable);
                }
                else
                {
                    throw new Exception($"Method {info.Name} must return IAwaitableSource<int>.");
                }
            }
            catch (TargetInvocationException tie)
            {
                var inner = tie.InnerException ?? tie;
                _ErrorDeserialize(info.Name, returnId, inner.Message);
            }
            catch (Exception e)
            {
                _ErrorDeserialize(info.Name, returnId, e.Message);
            }
        }

        private void _HandleStreamReturn(int methodId, long returnId, byte[] buffer, IAwaitableSource<int> awaitable)
        {
            if (awaitable == null)
            {
                _ErrorDeserialize(methodId.ToString(), returnId, "Stream method returned null awaitable.");
                return;
            }

            IAwaitable<int> awaiter;
            try
            {
                awaiter = awaitable.GetAwaiter();
            }
            catch (Exception e)
            {
                _ErrorDeserialize(methodId.ToString(), returnId, e.Message);
                return;
            }

            void Complete()
            {
                try
                {
                    int processCount = awaiter.GetResult();
                    _ReturnStreamValue(returnId, buffer, processCount);
                }
                catch (Exception e)
                {
                    _ErrorDeserialize(methodId.ToString(), returnId, e.Message);
                }
            }

            if (awaiter.IsCompleted)
            {
                Complete();
            }
            else
            {
                awaiter.OnCompleted(Complete);
            }
        }

        private void _ReturnStreamValue(long returnId, byte[] buffer, int processCount)
        {
            var length = processCount;
            if (length < 0)
            {
                length = 0;
            }
            if (buffer == null)
            {
                buffer = Array.Empty<byte>();
            }
            if (length > buffer.Length)
            {
                length = buffer.Length;
            }

            var data = new byte[length];
            if (length > 0)
            {
                System.Buffer.BlockCopy(buffer, 0, data, 0, length);
            }

            var package = new PackageReturnStreamMethod
            {
                ReturnTarget = returnId,
                ProcessCount = processCount,
                Buffer = data
            };
            _Queue.Push(ServerToClientOpCode.ReturnStreamMethod, _InternalSerializable.Serialize(package));
        }
    }

}
