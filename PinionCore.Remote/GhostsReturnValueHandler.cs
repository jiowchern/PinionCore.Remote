using System;
using PinionCore.Memorys;

namespace PinionCore.Remote
{
    namespace ProviderHelper
    {
        public class GhostsReturnValueHandler
        {
            private readonly ReturnValueQueue _ReturnValueQueue;
            private readonly ISerializable _Serializer;
            private readonly System.Collections.Generic.Dictionary<long, StreamContext> _StreamContexts;

            public event Action<string, string> ErrorMethodEvent;

            public bool CaptureCallerStack
            {
                get { return _ReturnValueQueue.CaptureCallerStack; }
                set { _ReturnValueQueue.CaptureCallerStack = value; }
            }

            public GhostsReturnValueHandler(ISerializable serializer)
            {
                _ReturnValueQueue = new ReturnValueQueue();
                _Serializer = serializer;
                _StreamContexts = new System.Collections.Generic.Dictionary<long, StreamContext>();
            }

            public void SetReturnValue(long returnTarget, byte[] returnValue)
            {
                IValue value = _ReturnValueQueue.PopReturnValue(returnTarget);
                if (value == null)
                {
                    // 遲到或重複的回應(對應的等待者已被錯誤回應清掉):忽略,不能毒死訊息迴圈
                    PinionCore.Utility.Log.Instance.WriteInfo($"SetReturnValue return value not found. return_target:{returnTarget}");
                    return;
                }
                var returnInstance = _Serializer.Deserialize(value.GetObjectType(), returnValue.AsBuffer());
                value.SetValue(returnInstance);
            }

            public void ErrorReturnValue(long returnTarget, string method, string message)
            {
                IValue value = _ReturnValueQueue.PopReturnValue(returnTarget, out var callerStack);
                _StreamContexts.Remove(returnTarget);
                // 讓等待中的 Value 以錯誤完成,呼叫端不再永久等待
                var text = string.IsNullOrEmpty(method) ? message : $"{method}: {message}";
                if (!string.IsNullOrEmpty(callerStack))
                    text = $"{text}\n--- RPC call site (captured at invocation) ---\n{callerStack}";
                value?.SetError(text);
                ErrorMethodEvent?.Invoke(method, message);
            }
            public IValue PopReturnValue(long return_id, IGhost ghost)
            {
                IValue value = _ReturnValueQueue.PopReturnValue(return_id);
                if (value != null)
                {
                    value.SetValue(ghost);
                }
                return value;
            }


            public IValue GetReturnValue(long returnId)
            {
                return _ReturnValueQueue.PopReturnValue(returnId);
            }

            internal long PushReturnValue(IValue return_value)
            {
                return _ReturnValueQueue.PushReturnValue(return_value);
            }

            internal long PushStreamReturn(Value<int> returnValue, byte[] buffer, int offset)
            {
                if (returnValue == null)
                    throw new System.ArgumentNullException(nameof(returnValue));
                if (buffer == null)
                    throw new System.ArgumentNullException(nameof(buffer));
                if (offset < 0 || offset > buffer.Length)
                    throw new System.ArgumentOutOfRangeException(nameof(offset));

                long id = _ReturnValueQueue.PushReturnValue(returnValue);
                _StreamContexts[id] = new StreamContext(buffer, offset);
                return id;
            }

            public void CompleteStreamReturn(long returnTarget, int processCount, byte[] data)
            {
                if (!_StreamContexts.TryGetValue(returnTarget, out StreamContext context))
                {
                    // 遲到或重複的回應:忽略,不能毒死訊息迴圈
                    PinionCore.Utility.Log.Instance.WriteInfo($"Stream return context not found. return_target:{returnTarget}");
                    return;
                }
                _StreamContexts.Remove(returnTarget);

                if (data != null && context.Buffer != null)
                {
                    int copyLength = data.Length;
                    if (processCount >= 0 && copyLength > processCount)
                    {
                        copyLength = processCount;
                    }
                    int remain = context.Buffer.Length - context.Offset;
                    if (copyLength > remain)
                    {
                        copyLength = remain;
                    }
                    if (copyLength > 0)
                    {
                        System.Buffer.BlockCopy(data, 0, context.Buffer, context.Offset, copyLength);
                    }
                }

                IValue value = _ReturnValueQueue.PopReturnValue(returnTarget);
                if (value == null)
                {
                    PinionCore.Utility.Log.Instance.WriteInfo($"Stream return value not found. return_target:{returnTarget}");
                    return;
                }
                value.SetValue(processCount);
            }

            private readonly struct StreamContext
            {
                public StreamContext(byte[] buffer, int offset)
                {
                    Buffer = buffer;
                    Offset = offset;
                }

                public byte[] Buffer { get; }
                public int Offset { get; }
            }
        }

    }
}
