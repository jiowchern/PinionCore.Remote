using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        public SoulMethodHandler(
            IRequestQueue peer,
            IResponseQueue queue,
            IProtocol protocol,
            ConcurrentDictionary<long, SoulProxy> souls,
            
            ISerializable serializer,
            IInternalSerializable internalSerializable)
        {
            _Peer = peer;
            _Queue = queue;
            _Protocol = protocol;
            _Souls = souls;
            _WaitValues = new Dictionary<long, IValue>();
            _Serializer = serializer;
            _InternalSerializable = internalSerializable;

            _Peer.InvokeMethodEvent += _InvokeMethod;
            _Peer.InvokeStreamMethodEvent += _InvokeStreamMethod;
        }

        public void Dispose()
        {
            _Peer.InvokeMethodEvent -= _InvokeMethod;
            _Peer.InvokeStreamMethodEvent -= _InvokeStreamMethod;
        }

        private void _InvokeMethod(long entity_id, int method_id, long returnId, byte[][] args)
        {
            SoulProxy soul;
            if (!_Souls.TryGetValue(entity_id, out soul))
            {
                throw new Exception($"Soul not found entity_id:{entity_id}");
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
                throw new Exception($"Method not found method_id:{method_id}");
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
            // 需要引用到 BindHandler，這裡假設有一個方法可以完成這個功能
            throw new NotImplementedException();
            // _bindHandler.Bind(returnValue.GetObject(), returnValue.GetObjectType(), true, returnId);
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
                throw new Exception($"Soul not found entity_id:{entity_id}");
            }

            MethodInfo info = _Protocol.GetMemberMap().GetMethod(method_id);
            if (info == null)
            {
                throw new Exception($"Stream method not found method_id:{method_id}");
            }

            try
            {
                var args = new object[] { buffer, 0, count };
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
